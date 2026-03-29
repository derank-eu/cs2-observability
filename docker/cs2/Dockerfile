# ── Stage 1: build the plugin ───────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS plugin-build

WORKDIR /src
COPY src/ .

# Publishing the Plugin project pulls in Core + OTel exporter as references.
RUN dotnet publish Cs2Observability.Plugin/Cs2Observability.Plugin.csproj \
      --configuration Release \
      --output /plugin/bin \
      --no-self-contained


# ── Stage 2: CS2 dedicated server with Metamod + CounterStrikeSharp ─────────
#
# joedwards32/cs2 installs the dedicated server at /home/steam/cs2.
# Pin the tag so the setup script versions stay consistent.
FROM joedwards32/cs2:latest

# Version pins — override at build time with --build-arg if newer versions are out.
# Metamod:Source 2.x for CS2/Source2:  https://mms.alliedmods.net/mmsdrop/2.0/
# CounterStrikeSharp releases:          https://github.com/roflmuffin/CounterStrikeSharp/releases
ARG METAMOD_BUILD=1309
ARG CSS_VERSION=282

ENV GAME_ROOT=/home/steam/cs2/game/csgo
ENV CSS_ROOT=${GAME_ROOT}/addons/counterstrikesharp

USER root

RUN apt-get update \
 && apt-get install -y --no-install-recommends wget tar unzip \
 && rm -rf /var/lib/apt/lists/*

# ── Install Metamod:Source ───────────────────────────────────────────────────
RUN wget -q \
      "https://mms.alliedmods.net/mmsdrop/2.0/mmsource-2.0.0-git${METAMOD_BUILD}-linux.tar.gz" \
      -O /tmp/metamod.tar.gz \
 && tar -xzf /tmp/metamod.tar.gz -C "${GAME_ROOT}/" \
 && rm /tmp/metamod.tar.gz

# ── Install CounterStrikeSharp (includes its own .NET runtime) ───────────────
RUN wget -q \
      "https://github.com/roflmuffin/CounterStrikeSharp/releases/download/v${CSS_VERSION}/CounterStrikeSharp_with_runtime_v${CSS_VERSION}_linux-x64.tar.gz" \
      -O /tmp/css.tar.gz \
 && tar -xzf /tmp/css.tar.gz -C "${GAME_ROOT}/" \
 && rm /tmp/css.tar.gz

# ── Install the plugin ───────────────────────────────────────────────────────
COPY --from=plugin-build /plugin/bin/ \
     ${CSS_ROOT}/plugins/Cs2Observability/

# ── Pre-configure the plugin (OTLP → otel-collector service on the compose network)
COPY docker/cs2/plugin-config/ \
     ${CSS_ROOT}/configs/plugins/Cs2Observability/

RUN chown -R steam:steam "${GAME_ROOT}/addons/"

USER steam
