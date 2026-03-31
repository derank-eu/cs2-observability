#!/usr/bin/env bash
# scripts/wsl-port-forward.sh
#
# Creates a UDP+TCP tunnel from the Windows host into WSL2 for the CS2 server.
#
# WHY: WSL2 runs inside a Hyper-V VM with its own NAT. `netsh interface portproxy`
# handles TCP forwarding natively, but has no UDP support. CS2 game traffic is
# UDP, so incoming connections never reach Docker without this relay.
#
# HOW: This script:
#   1. Reads the WSL2 IP automatically (changes on every WSL restart).
#   2. Adds a netsh TCP portproxy rule (Windows built-in).
#   3. Opens the Windows Firewall for UDP + TCP on the CS2 port.
#   4. Launches a PowerShell UDP relay (uses inline C# for proper bidirectional
#      per-client relay — each external client gets its own relay socket so
#      response packets are routed back to the correct source).
#
# MODERN ALTERNATIVE (Windows 11 22H2+ / WSL 2.0+):
#   Enable mirrored networking in ~/.wslconfig and skip this script entirely:
#
#     [wsl2]
#     networkingMode=mirrored
#
#   Then run: wsl.exe --shutdown && wsl.exe
#
# REQUIREMENTS:
#   - WSL2 (tested on Ubuntu)
#   - powershell.exe on PATH (standard WSL2 setup)
#   - Windows admin rights (netsh + firewall need elevation)
#
# USAGE:
#   ./scripts/wsl-port-forward.sh           # start forwarding
#   ./scripts/wsl-port-forward.sh --stop    # tear everything down

set -euo pipefail

PORT="${CS2_PORT:-27015}"
RULE_NAME="CS2-Observability"
PS_RELAY_WSL="/tmp/cs2-udp-relay.ps1"
PID_FILE="/tmp/cs2-udp-relay.pid"

# ---- helpers ----------------------------------------------------------------

die()  { echo "ERROR: $*" >&2; exit 1; }
info() { echo "  $*"; }

need_powershell() {
    command -v powershell.exe &>/dev/null \
        || die "powershell.exe not found. This script must be run from WSL2."
}

wsl_ip() {
    # Primary WSL2 interface IP (the one Docker binds to).
    hostname -I | awk '{print $1}'
}

# Run a PowerShell snippet and print its output.
run_ps() {
    powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$1" 2>&1 \
        | sed 's/\r//'  # strip Windows CR
}

# ---- stop -------------------------------------------------------------------

do_stop() {
    echo "Removing CS2 port-forwarding rules..."

    run_ps "
        \$e = \$ErrorActionPreference; \$ErrorActionPreference = 'SilentlyContinue'
        netsh interface portproxy delete v4tov4 listenport=$PORT listenaddress=0.0.0.0
        netsh advfirewall firewall delete rule name='$RULE_NAME'
        \$ErrorActionPreference = \$e
        Write-Host 'Windows rules removed.'
    "

    if [[ -f "$PID_FILE" ]]; then
        local pid
        pid=$(cat "$PID_FILE")
        if kill "$pid" 2>/dev/null; then
            info "UDP relay (PID $pid) stopped."
        else
            info "UDP relay was not running."
        fi
        rm -f "$PID_FILE"
    fi

    rm -f "$PS_RELAY_WSL"
    echo "Done."
}

# ---- start ------------------------------------------------------------------

write_relay_script() {
    local wsl_ip="$1"

    # NOTE: This is a bash heredoc (unquoted PSEOF so $PORT / $wsl_ip expand).
    # PowerShell variables are escaped with \ to survive bash expansion.
    # The inline C# here-string avoids PowerShell threading/closure pitfalls.
    cat > "$PS_RELAY_WSL" << PSEOF
# CS2 UDP relay: forwards incoming UDP on 0.0.0.0:$PORT to WSL2 ($wsl_ip:$PORT)
# and routes server responses back to the correct external client.

Add-Type -TypeDefinition @"
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

public class UdpRelay {
    private readonly Socket        _front;
    private readonly IPEndPoint    _backend;
    private readonly Dictionary<string, Socket> _relays
        = new Dictionary<string, Socket>();

    public UdpRelay(int port, string backendIp) {
        _front   = new Socket(AddressFamily.InterNetwork,
                              SocketType.Dgram, ProtocolType.Udp);
        _front.Bind(new IPEndPoint(IPAddress.Any, port));
        _backend = new IPEndPoint(IPAddress.Parse(backendIp), port);
    }

    public void Run() {
        var buf = new byte[65535];
        Console.WriteLine("UDP relay: 0.0.0.0:" + _backend.Port
                          + " <-> " + _backend);
        while (true) {
            EndPoint from = new IPEndPoint(IPAddress.Any, 0);
            int n    = _front.ReceiveFrom(buf, ref from);
            var data = new byte[n];
            Array.Copy(buf, data, n);

            string key = from.ToString();
            Socket relay;
            lock (_relays) {
                if (!_relays.TryGetValue(key, out relay)) {
                    relay = new Socket(AddressFamily.InterNetwork,
                                       SocketType.Dgram, ProtocolType.Udp);
                    relay.Bind(new IPEndPoint(IPAddress.Any, 0));
                    _relays[key] = relay;

                    // Capture locals for the response thread.
                    var clientSnap = from;
                    var relayCopy  = relay;
                    var t = new Thread(() => RelayResponses(relayCopy, clientSnap));
                    t.IsBackground = true;
                    t.Start();
                }
            }
            relay.SendTo(data, _backend);
        }
    }

    // Reads packets from WSL2 and sends them back to the original client.
    private void RelayResponses(Socket relay, EndPoint client) {
        var buf = new byte[65535];
        while (true) {
            try {
                EndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                int n    = relay.ReceiveFrom(buf, ref ep);
                var data = new byte[n];
                Array.Copy(buf, data, n);
                _front.SendTo(data, client);
            } catch { return; }
        }
    }
}
"@

\$relay = [UdpRelay]::new($PORT, '$wsl_ip')
\$relay.Run()
PSEOF
}

do_start() {
    need_powershell

    if [[ -f "$PID_FILE" ]] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
        die "Relay already running (PID $(cat "$PID_FILE")). Run --stop first."
    fi

    local ip
    ip=$(wsl_ip)

    echo "Starting CS2 port forwarding..."
    info "WSL2 IP : $ip"
    info "Port    : $PORT (UDP + TCP)"
    echo ""

    # 1. TCP portproxy via netsh
    echo "[1/3] TCP portproxy..."
    run_ps "
        \$e = \$ErrorActionPreference; \$ErrorActionPreference = 'SilentlyContinue'
        netsh interface portproxy delete v4tov4 listenport=$PORT listenaddress=0.0.0.0
        \$ErrorActionPreference = \$e
        netsh interface portproxy add v4tov4 \`
            listenport=$PORT listenaddress=0.0.0.0 \`
            connectport=$PORT connectaddress=$ip
        Write-Host 'TCP portproxy: OK'
    "

    # 2. Firewall rules
    echo "[2/3] Windows Firewall..."
    run_ps "
        \$e = \$ErrorActionPreference; \$ErrorActionPreference = 'SilentlyContinue'
        netsh advfirewall firewall delete rule name='$RULE_NAME'
        \$ErrorActionPreference = \$e
        netsh advfirewall firewall add rule name='$RULE_NAME' protocol=UDP dir=in localport=$PORT action=allow
        netsh advfirewall firewall add rule name='$RULE_NAME' protocol=TCP dir=in localport=$PORT action=allow
        Write-Host 'Firewall rules: OK'
    "

    # 3. UDP relay (PowerShell/C# process)
    echo "[3/3] UDP relay..."
    write_relay_script "$ip"

    local ps_win
    ps_win=$(wslpath -w "$PS_RELAY_WSL")

    powershell.exe -NoProfile -NonInteractive \
        -ExecutionPolicy Bypass \
        -File "$ps_win" &
    echo $! > "$PID_FILE"

    info "UDP relay started (PID $(cat "$PID_FILE"))"
    echo ""
    echo "Forwarding active. To stop: $0 --stop"
    echo ""
    echo "NOTE: netsh commands require Windows admin rights."
    echo "      If rules were not applied, re-run from an elevated WSL terminal"
    echo "      or run PowerShell as Administrator and execute:"
    echo "        netsh interface portproxy show all"
}

# ---- main -------------------------------------------------------------------

case "${1:-}" in
    --stop|-s|stop) do_stop  ;;
    --help|-h)
        sed -n '2,/^set /p' "$0" | grep '^#' | sed 's/^# \?//'
        ;;
    *)  do_start ;;
esac
