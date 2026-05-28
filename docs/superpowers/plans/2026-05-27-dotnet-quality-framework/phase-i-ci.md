# Phase I — CI — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this phase task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Overview:** [`00-overview.md`](00-overview.md) holds the goal, architecture, tech stack, file structure, and shipping points referenced by every phase.

## Phase I — CI

### Task 26: `docker/Dockerfile.quality`

**Files:**
- Create: `docker/Dockerfile.quality`

- [ ] **Step 1: Write the Dockerfile** — pinned SDK plus pinned external scanners.

```dockerfile
# Pinned quality image used by CI hermetic job.
FROM mcr.microsoft.com/dotnet/sdk:9.0.100

ARG GITLEAKS_VERSION=8.21.0
ARG TRIVY_VERSION=0.56.2
ARG SEMGREP_VERSION=1.92.0
ARG OSV_VERSION=1.9.0

RUN apt-get update && apt-get install -y --no-install-recommends \
        bash curl ca-certificates git python3-pip pre-commit jq \
    && rm -rf /var/lib/apt/lists/*

RUN curl -sSL -o /usr/local/bin/gitleaks.tgz \
      https://github.com/gitleaks/gitleaks/releases/download/v${GITLEAKS_VERSION}/gitleaks_${GITLEAKS_VERSION}_linux_x64.tar.gz \
 && tar -xzf /usr/local/bin/gitleaks.tgz -C /usr/local/bin gitleaks \
 && rm /usr/local/bin/gitleaks.tgz

RUN curl -sSL https://github.com/aquasecurity/trivy/releases/download/v${TRIVY_VERSION}/trivy_${TRIVY_VERSION}_Linux-64bit.tar.gz \
   | tar -xz -C /usr/local/bin trivy

RUN curl -sSL -o /usr/local/bin/osv-scanner \
     https://github.com/google/osv-scanner/releases/download/v${OSV_VERSION}/osv-scanner_linux_amd64 \
 && chmod +x /usr/local/bin/osv-scanner

RUN pip3 install --no-cache-dir semgrep==${SEMGREP_VERSION}

WORKDIR /repo
ENTRYPOINT ["bash"]
```

- [ ] **Step 2: Build locally.**

Run: `docker build -f docker/Dockerfile.quality -t quality-tools:pinned .`
Expected: image builds.

- [ ] **Step 3: Smoke run.**

Run: `docker run --rm quality-tools:pinned -lc 'gitleaks version && trivy --version && osv-scanner --version && semgrep --version && dotnet --version'`
Expected: prints all five versions.

- [ ] **Step 4: Commit.**

```bash
git add docker/Dockerfile.quality
git commit -m "ci: add pinned quality-tools Docker image"
```

### Task 27: `.github/workflows/quality.yml`

**Files:**
- Create: `.github/workflows/quality.yml`

- [ ] **Step 1: Write workflow** mirroring spec lines 372–392.

```yaml
name: quality

on:
  push:
    branches: [main]
  pull_request:

jobs:
  quality-native:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json
      - name: Cache NuGet
        uses: actions/cache@v4
        with:
          path: |
            ~/.nuget/packages
            ~/.dotnet/tools
          key: ${{ runner.os }}-nuget-${{ hashFiles('.config/dotnet-tools.json', '**/packages.lock.json') }}
      - run: dotnet tool restore
      - uses: actions/setup-python@v5
        with: { python-version: '3.12' }
      - run: pip install pre-commit==3.8.0
      - uses: pre-commit/action@v3.0.1
      - run: dotnet quality pr-check

  quality-docker:
    runs-on: ubuntu-latest
    if: github.event_name == 'pull_request'
    steps:
      - uses: actions/checkout@v4
      - name: Build pinned image
        run: docker build -f docker/Dockerfile.quality -t quality-tools:pinned .
      - name: Run pr-check inside container
        run: |
          docker run --rm -v "$PWD:/repo" -w /repo quality-tools:pinned \
            -lc 'dotnet tool restore && dotnet quality pr-check'
```

- [ ] **Step 2: Commit.**

```bash
git add .github/workflows/quality.yml
git commit -m "ci: add quality workflow (native + docker)"
```

> Verification of CI deferred to first PR — local Phase G smoke covers behavior.
