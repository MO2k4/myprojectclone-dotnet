# CI ↔ Local Tooling Version Alignment

**Date:** 2026-05-28
**Status:** Approved (brainstorming)
**Scope:** `.github/workflows/quality.yml`, `docker/Dockerfile.quality`, new `.mise.toml`

## Problem

Tool versions drift between the developer's machine and CI because they're
pinned in different places — or not pinned at all. Today's snapshot:

| Tool | Local (Mac) | CI runner | Source of CI pin |
|---|---|---|---|
| semgrep | 1.157.0 | 1.92.0 | inline `pip install` in `quality.yml` |
| jscpd | 4.2.4 | 4.0.5 | inline `npm install -g` in `quality.yml` |
| pre-commit | 4.6.0 | 3.8.0 | inline `pip install` in `quality.yml` |
| trivy | 0.70.0 | 0.70.0 | inline `curl` in `quality.yml` + `ARG` in `Dockerfile.quality` |
| Python | 3.14.5 | 3.12.x | `actions/setup-python@v5` |
| Node / npm | 25.9.0 / 11.12.1 | runner default (~20.x) | not pinned |

Tools already drift-free (pinned by mechanisms that work):

| Tool | Version | Pin location |
|---|---|---|
| gitleaks | 8.21.0 | `.pre-commit-config.yaml` (`rev:`) + `Dockerfile.quality` (`ARG`) |
| osv-scanner | 2.3.8 | `.pre-commit-config.yaml` (`rev:`) + `Dockerfile.quality` (`ARG`) |
| editorconfig-checker | 3.0.3 | `.pre-commit-config.yaml` (`rev:`) |
| dotnet-project-licenses | 2.7.1 | `.config/dotnet-tools.json` |
| dotnet-quality | 0.1.0 | `.config/dotnet-tools.json` |

.NET SDK is explicitly **out of scope** — it stays pinned in `global.json` and
controlled by `DOTNET_ROLL_FORWARD`.

## Goals

1. Bump CI to today's local versions for the four drifting tools (semgrep,
   jscpd, pre-commit, trivy).
2. Establish a single source of truth so any future bump on one side
   propagates to the other.
3. Eliminate `Dockerfile.quality` as a duplicate pin location for the same
   four tools.

## Non-goals

- Pinning the .NET SDK (out of scope per user direction; `global.json` is canonical).
- Pinning the Python interpreter or Node runtime in `.mise.toml`. Both stay
  host-provided. The host on CI is whatever `ubuntu-latest` ships.
- Moving gitleaks / osv-scanner / editorconfig-checker into `.mise.toml`.
  Pre-commit's `rev:` already keeps them drift-free; adding a second pin
  location would be a regression.
- Moving `dotnet-project-licenses` into `.mise.toml`. `.config/dotnet-tools.json`
  is the idiomatic place for .NET tools and is already aligned.
- Adding automated bump PRs (Renovate / Dependabot). Possible follow-up; not
  required to satisfy the alignment goal.
- Adding a `mise.lock`. The version pins are enough for these tools; bit-exact
  reproducibility is not a project requirement.

## Solution

### New file: `.mise.toml`

```toml
[tools]
"pipx:semgrep" = "1.157.0"
"pipx:pre-commit" = "4.6.0"
"npm:jscpd" = "4.2.4"
trivy = "0.70.0"
```

This file is the single source of truth for the four drifting tools.

### Three consumers

```
                  .mise.toml
                  /    |    \
                 /     |     \
                /      |      \
   quality.yml   Dockerfile.quality   local dev (Mac)
   (jdx/mise-     (RUN mise install)   (mise install)
    action@v2)
```

### `.github/workflows/quality.yml` changes

Replace the `pip install`, `npm install -g`, and `curl trivy` lines with one
`jdx/mise-action@v2` step. Keep `setup-dotnet`. Remove `setup-python` — mise's
`pipx:` backend pulls a working Python.

Concretely, this block:

```yaml
- uses: actions/setup-python@v5
  with: { python-version: '3.12' }
- run: pip install "setuptools<81" pre-commit==3.8.0 semgrep==1.92.0
- name: Install external scanners (trivy, jscpd)
  run: |
    curl -sSL https://github.com/aquasecurity/trivy/releases/download/v0.70.0/trivy_0.70.0_Linux-64bit.tar.gz \
      | sudo tar -xz -C /usr/local/bin trivy
    sudo npm install -g jscpd@4.0.5
```

becomes:

```yaml
- uses: jdx/mise-action@v2
  with:
    cache: true
```

The existing `uses: pre-commit/action@v3.0.1` step is removed and replaced
with a direct `mise exec` call, since `pre-commit/action` would re-install
its own pre-commit via pip and bypass the mise-pinned version:

```yaml
- run: mise exec -- pre-commit run --all-files --show-diff-on-failure
  env:
    SKIP: no-commit-to-branch
```

The trailing `dotnet quality pr-check` step is unchanged.

The `setuptools<81` constraint disappears with this change — it was a
workaround for semgrep 1.92.0 importing `pkg_resources`. Semgrep 1.157.0 does
not need it.

### `docker/Dockerfile.quality` changes

Remove the four `ARG` lines and `RUN` blocks for trivy / semgrep (gitleaks
and osv-scanner stay — they're not in `.mise.toml`). Add a mise install step
that consumes the same `.mise.toml`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0.100

ARG GITLEAKS_VERSION=8.21.0
ARG OSV_VERSION=1.9.0

RUN apt-get update && apt-get install -y --no-install-recommends \
        bash curl ca-certificates git python3 python3-pip python3-venv \
        nodejs npm jq \
    && rm -rf /var/lib/apt/lists/*

# Gitleaks + osv-scanner stay outside mise (pinned by pre-commit rev: in repo).
RUN curl -sSL -o /usr/local/bin/gitleaks.tgz \
      https://github.com/gitleaks/gitleaks/releases/download/v${GITLEAKS_VERSION}/gitleaks_${GITLEAKS_VERSION}_linux_x64.tar.gz \
 && tar -xzf /usr/local/bin/gitleaks.tgz -C /usr/local/bin gitleaks \
 && rm /usr/local/bin/gitleaks.tgz

RUN curl -sSL -o /usr/local/bin/osv-scanner \
     https://github.com/google/osv-scanner/releases/download/v${OSV_VERSION}/osv-scanner_linux_amd64 \
 && chmod +x /usr/local/bin/osv-scanner

# Mise owns trivy, semgrep, pre-commit, jscpd.
RUN curl https://mise.run | sh
ENV PATH="/root/.local/bin:/root/.local/share/mise/shims:${PATH}"

WORKDIR /repo
COPY .mise.toml /repo/.mise.toml
RUN mise trust /repo/.mise.toml && mise install

ENTRYPOINT ["bash"]
```

The `mise trust` line is required because mise refuses to auto-execute config
files unless they're explicitly trusted (security guard).

The `quality-docker` job in `quality.yml` keeps invoking the image the same
way it does today; only the image's internals change.

### `.pre-commit-config.yaml` — unchanged

Gitleaks (`v8.21.0`), osv-scanner (`v2.3.8`), and editorconfig-checker
(`3.0.3`) stay pinned in `rev:` blocks. Pre-commit downloads them itself.
Local and CI get the same version because pre-commit is the same version on
both sides — and pre-commit itself is now pinned by `.mise.toml`.

### `.config/dotnet-tools.json` — unchanged

`dotnet-project-licenses` (2.7.1) and `dotnet-quality` (0.1.0) stay where
they are. They're already aligned and the .NET tool manifest is the right
home for them.

### `global.json` — unchanged

`.NET SDK 9.0.100` with `rollForward: latestFeature` stays as-is. Out of
scope.

### Local dev workflow

After this lands, the bootstrap sequence on a fresh clone becomes:

```bash
brew install mise                       # one-time, per machine
mise install                            # one-time per clone, re-run after .mise.toml changes
mise exec -- pre-commit install         # wires git hooks via mise-pinned pre-commit
dotnet tool restore                     # unchanged
```

Or, with `eval "$(mise activate zsh)"` in `~/.zshrc` (recommended), the
`mise exec --` prefix can be dropped because mise's shims are on `PATH`:

```bash
pre-commit install                      # uses mise's pre-commit transparently
```

The `mise install` call replaces ad-hoc `pip install semgrep`, `npm install -g
jscpd`, and `brew install trivy` steps. README / AGENTS.md gets a short
section documenting both paths.

### Bumping versions in the future

On the developer's Mac:

```bash
mise upgrade semgrep     # rewrites .mise.toml
git diff .mise.toml      # inspect
git add .mise.toml && git commit
```

CI and Docker pick up the new version on the next run because both pull from
the same `.mise.toml`.

## Risks and mitigations

1. **mise availability.** Adds `brew install mise` as a one-time dev
   dependency on macOS and `curl https://mise.run | sh` on the Linux
   container. Both are well-established install paths. The
   `jdx/mise-action@v2` GitHub Action is widely used and maintained by
   mise's author.

2. **`pipx:` backend maturity.** mise's pipx backend requires a recent mise
   release. Verify it works in CI before merging. **Fallback if it doesn't:**
   pin Python in `.mise.toml` and use a `[tasks]` block to `pip install`
   pinned packages. Uglier but functional.

3. **Windows-native developers.** Not in scope today, but worth noting: mise
   itself runs on Windows, and trivy/jscpd/pre-commit work there. Semgrep on
   Windows is not officially supported by semgrep itself — this is a semgrep
   limitation that already affects the current setup, not a regression.

4. **Docker build cache invalidation.** Editing `.mise.toml` re-runs `mise
   install` during image build. That's correct behavior; the trade-off is a
   slower first build after a bump.

5. **No enforcement of alignment.** A developer could still `pip install
   semgrep` outside mise and shadow the pinned binary. Out of scope for this
   spec; a future `dotnet quality check tool-versions` hook could detect
   drift if desired.

6. **`setuptools<81` workaround removal.** The current workflow pins
   `setuptools<81` to let semgrep 1.92.0 import `pkg_resources`. Semgrep
   1.157.0 dropped that dependency, so the pin can go. Validate that
   `mise exec -- semgrep --version` works in CI before declaring done.

## Acceptance criteria

- `.mise.toml` exists at repo root and pins exactly: semgrep 1.157.0,
  pre-commit 4.6.0, jscpd 4.2.4, trivy 0.70.0.
- `.github/workflows/quality.yml` no longer contains literal version numbers
  for any of those four tools.
- `docker/Dockerfile.quality` no longer contains `ARG TRIVY_VERSION=` or
  `ARG SEMGREP_VERSION=`; it builds by reading `.mise.toml`.
- A `quality.yml` run on the resulting branch passes both the `quality-native`
  and `quality-docker` jobs.
- `mise exec -- semgrep --version` on the runner reports 1.157.0.
- `mise exec -- jscpd --version` reports 4.2.4.
- `mise exec -- pre-commit --version` reports 4.6.0.
- `mise exec -- trivy --version` reports 0.70.0.

## Files to change

| File | Type of change |
|---|---|
| `.mise.toml` | New |
| `.github/workflows/quality.yml` | Replace pip/npm/curl steps with `jdx/mise-action`; wrap `pre-commit run` in `mise exec` |
| `docker/Dockerfile.quality` | Drop trivy/semgrep `ARG` + `RUN`; add `mise` install + `COPY .mise.toml` + `mise install` |
| `README.md` (or `AGENTS.md`) | Add `brew install mise && mise install` to bootstrap steps |
