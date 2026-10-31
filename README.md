# Mad Catz R.A.T. 6+ for Linux

A Linux application for the Mad Catz R.A.T. 6+ (`12cf:0c04`): profile selection,
four DPI stages, and per-zone RGB lighting.

## Features

- Reads and writes the four on-board profiles.
- Four DPI stages with 100 DPI steps (100–10000).
- Three LED zones with solid, breathing, and rainbow modes.
- Color picker, brightness, and breathing period.
- Profile activation and automatic backup before writing.
- Localized UI: English, Russian, German, French, Chinese.
- CLI for scripting: `info`, `dump`, `backup`, `dpi`, `led`, `restore`, `select`.

## AppImage

Download `Rat6-<version>-x86_64.AppImage` from the releases page of either
repository — the file is the same:

- GitLab (main repository): `homelab/rat-linux` → Releases;
- GitHub (mirror): <https://github.com/wolfDiesel/mad-catz-rat-linux/releases>.

```bash
chmod +x Rat6-<version>-x86_64.AppImage
./Rat6-<version>-x86_64.AppImage
```

The application needs access to the mouse hidraw interface. On the first access
it asks for administrator privileges through the normal polkit dialog. No udev
rules, policy packages, or system helpers are installed. The AppImage stages
its helper in `~/.local/share/rat6` so pkexec can execute it outside the
AppImage FUSE mount.

## Releases and CI

GitLab is the main repository: `origin` is
`ssh://git@gitlab.localnet:30222/homelab/rat-linux.git`, and GitHub is a push
mirror refreshed by GitLab. Both CIs build the AppImage from the same recipe,
`packaging/appimage/build-appimage.sh`:

| Where | What it builds | What it publishes |
|---|---|---|
| GitLab `.gitlab-ci.yml` | `dotnet test`, `dotnet build`, AppImage — on `main`, merge requests and `v*` tags | junit test report and AppImage as job artifacts; GitLab release on a tag |
| GitHub `.github/workflows/ci.yml` | the same tests on `main`, pull requests and `v*` tags; the same AppImage on `v*` tags, after the tests pass | test report as a workflow artifact; GitHub release with the AppImage attached |

Release a version:

1. set `<Version>` in `src/Rat6.App/Rat6.App.csproj` and commit;
2. push to GitLab: `git push origin main`;
3. tag and push the tag: `git tag vX.Y.Z && git push origin vX.Y.Z` — the tag
   and `<Version>` must match, otherwise the GitLab `version` job fails before
   anything is built;
4. GitLab builds the AppImage and creates the GitLab release; the mirror pushes
   the tag to GitHub, where the same tag starts the GitHub workflow and the
   GitHub release.

## Tests

The test project (`tests/Rat6.Tests`) covers the protocol frames, profile
serialization, device operations replayed from the recorded profile fixtures in
`tests/Rat6.Tests/Fixtures`, and the helper contract. No mouse is required.

```bash
dotnet test tests/Rat6.Tests/Rat6.Tests.csproj
```

GitLab CI runs the same command in the `test` stage on `main`, on merge
requests and on `v*` tags, with the JUnit logger from `JunitXml.TestLogger`: the
report fills the pipeline's **Tests** tab and surfaces in merge requests. The
GitHub workflow runs the same tests and uploads the report as an artifact; the
AppImage job waits for them, so a tag with failing tests does not produce a
release.

## Build from source

Requirements:

- .NET 10 SDK;
- `librsvg2-bin` for rendering the icon;
- `desktop-file-utils` for validating the desktop entry;
- `curl` for downloading `appimagetool`.

```bash
dotnet build Rat6.slnx
./packaging/appimage/build-appimage.sh
```

The result is written to `dist/Rat6-<version>-x86_64.AppImage`.

To build a specific version:

```bash
APPIMAGE_VERSION=1.0.1 ./packaging/appimage/build-appimage.sh
```

## Project layout

```text
src/
  Rat6.Protocol/          profile model and protocol frames
  Rat6.Transport/         transport abstraction
  Rat6.Transport.Linux/   hidraw transport and pkexec helper client
  Rat6.Helper.Contract/   helper JSON contract
  Rat6.Helper/            privileged helper process
  Rat6.Device/            device operations and backups
  Rat6.Cli/               command-line interface
  Rat6.App/               Avalonia application
tests/                    xunit tests and profile fixtures
packaging/appimage/       AppImage build files
Rat6.slnx
```

## Writing safely

Writing is opt-in:

- the GUI requires the "Allow writing to mouse" checkbox;
- the CLI requires `--apply` or `--yes`;
- a backup of all four profiles is created before the first write;
- after writing, the app reloads and verifies the profile by reading it back.

## CLI

```bash
rat6 info
rat6 dump
rat6 backup
rat6 dpi --slot 0 --values 800,1600,3200,10000 --apply
rat6 led --slot 0 --zone all --mode breathing --color 0000FF --brightness 128 --period 6 --apply
rat6 restore --dir <backup> --yes
rat6 select --slot 1 --yes
```

Without `--apply`/`--yes`, commands only read and print the plan.

## License

GPL-3.0. See [LICENSE](LICENSE).
