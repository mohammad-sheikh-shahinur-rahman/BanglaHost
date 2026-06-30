# BanglaHost for Windows

Native Windows build of BanglaHost (C# / .NET 8 + WinUI 3). See the full architecture
and port mapping in [`../docs/WINDOWS-PORT.md`](../docs/WINDOWS-PORT.md).

> Build and test this **on Windows** — WinUI, Windows services, the hosts file, and
> the installer can't be built or verified on macOS/Linux.

---

# 📖 User Guide (A–Z) · ব্যবহার নির্দেশিকা

> **What is BanglaHost?** A one-click local web hosting environment for Windows —
> nginx/Apache + PHP + MySQL/MariaDB + HTTPS, all managed from a single dashboard.
> Think "Laragon / Laravel Herd", made simple.
>
> 🇧🇩 **BanglaHost কী?** Windows-এর জন্য এক-ক্লিকে লোকাল ওয়েব হোস্টিং পরিবেশ — nginx/Apache
> + PHP + MySQL/MariaDB + HTTPS, সবকিছু একটা ড্যাশবোর্ড থেকে নিয়ন্ত্রণ করা যায়। অর্থাৎ
> আপনার নিজের কম্পিউটারেই ওয়েবসাইট বানিয়ে চালাতে পারবেন।

### 1. Install · ইনস্টল করা

1. Download **`BanglaHost-Setup-1.1.0.exe`** from the [Releases page](https://github.com/mohammad-sheikh-shahinur-rahman/BanglaHost/releases).
2. Double-click it. Windows SmartScreen may show *"Windows protected your PC"* → click **More info → Run anyway** (the app is unsigned, this is normal & safe).
3. Follow the wizard → BanglaHost installs into **Program Files** and adds a Start-menu + desktop shortcut.

> 🇧🇩 **ধাপ:** Releases পেজ থেকে `BanglaHost-Setup-1.1.0.exe` ডাউনলোড করুন → ডাবল-ক্লিক করুন →
> SmartScreen এলে **More info → Run anyway** চাপুন (অ্যাপটি unsigned, এটা স্বাভাবিক) →
> Next চেপে চেপে ইনস্টল শেষ করুন। ইনস্টলের পর Start মেনু ও ডেস্কটপে শর্টকাট পাবেন।

### 2. First launch · প্রথমবার চালু করা

- Open **BanglaHost** from the Start menu. You land on the **Dashboard**.
- Click **Start all** to launch the web stack (nginx, PHP, MySQL). The **first start shows a UAC prompt** — this is BanglaHost editing your `hosts` file so `*.test` domains resolve locally. Click **Yes**.

> 🇧🇩 Start মেনু থেকে **BanglaHost** খুলুন → **Dashboard** আসবে → **Start all** চাপলে nginx, PHP,
> MySQL চালু হবে। প্রথমবার একটা **UAC (Admin) অনুমতি** চাইবে — এটা `hosts` ফাইল ঠিক করার জন্য,
> **Yes** চাপুন।

### 3. Create your first site · প্রথম সাইট তৈরি

1. Go to the **Sites** page → click **Add site**.
2. Enter a **name** (e.g. `myshop`), pick a **PHP version**, the **web server** (nginx or Apache), and a **document root** folder.
3. Save → your site is instantly live at **`http://myshop.test`**. Click **Open** to view it in the browser.

| Field | Meaning | বাংলা |
|---|---|---|
| **Name** | Becomes `name.test` | সাইটের নাম, এটাই হবে `name.test` |
| **PHP** | PHP version for this site | এই সাইটের PHP ভার্সন |
| **Server** | nginx (default) or Apache | কোন ওয়েব সার্ভার চলবে |
| **Document root** | Folder with your code | কোডের ফোল্ডার |

> 🇧🇩 **Sites** পেজে যান → **Add site** → নাম দিন (যেমন `myshop`), PHP ভার্সন, সার্ভার ও
> ফোল্ডার বেছে নিন → Save। সাথে সাথে সাইট চালু হবে **`http://myshop.test`** ঠিকানায়। **Open**
> চাপলে ব্রাউজারে খুলবে।

### 4. Enable HTTPS · এইচটিটিপিএস (নিরাপদ 🔒) চালু করা

- On any site row, click **Secure** (the 🔒 action). BanglaHost uses **mkcert** to issue a locally-trusted certificate and reloads nginx — your site now works at **`https://myshop.test`** with no browser warning.

> 🇧🇩 যেকোনো সাইটের পাশে **Secure (🔒)** চাপুন → mkcert দিয়ে একটা বিশ্বস্ত সার্টিফিকেট বানিয়ে দেবে,
> ফলে **`https://myshop.test`** ঠিকানা নিরাপদভাবে (warning ছাড়া) চলবে।

### 5. One-click apps: WordPress & Laravel · এক-ক্লিকে অ্যাপ

- Right-click a site → **Install app** → **WordPress** or **Laravel**.
  - **WordPress**: downloads the latest version, extracts it, and **auto-fills `wp-config.php`** with the site's database — just open the site and finish the famous 5-minute setup.
  - **Laravel**: runs **`composer create-project`** and wires its `.env` to your local MySQL automatically.

> 🇧🇩 সাইটে **রাইট-ক্লিক → Install app → WordPress / Laravel**।
> **WordPress** নিজেই সর্বশেষ ভার্সন নামিয়ে ডাটাবেস কনফিগারসহ বসিয়ে দেয়; **Laravel** composer দিয়ে
> নতুন প্রজেক্ট বানিয়ে `.env` ঠিক করে দেয়। আপনাকে আলাদা কিছু করতে হবে না।

### 6. Databases · ডাটাবেস

- The **Databases** page lets you **create / list / drop** MySQL databases. Root is on `127.0.0.1:3306` (passwordless by default).
- Click **Adminer** for a full web-based database UI at `adminer.test`.

> 🇧🇩 **Databases** পেজ থেকে ডাটাবেস **তৈরি / তালিকা / মুছে** ফেলা যায়। **Adminer** চাপলে
> ব্রাউজারে পূর্ণাঙ্গ ডাটাবেস ম্যানেজার (`adminer.test`) খুলবে।

### 7. Edit `.env` & open in editor/terminal · `.env` এডিট ও এডিটর/টার্মিনাল

- Right-click a site for handy actions: **Edit `.env`** (inline editor with Save), **Open in editor** (VS Code / Cursor / Sublime / Notepad++), **Open terminal**, **Open folder**, and **View logs**.

> 🇧🇩 সাইটে রাইট-ক্লিক করে পাবেন: **Edit `.env`** (সরাসরি এডিট ও Save), **Open in editor**
> (VS Code/Cursor ইত্যাদি), **Open terminal**, **Open folder**, **View logs**।

### 8. PHP versions & services · PHP ভার্সন ও সার্ভিস

- Change a site's PHP from its menu (**Change PHP**), or set the default in **Settings**.
- The **Services** page starts/stops individual services (nginx, PHP, MySQL, Redis, Mailpit, etc.). **Mailpit** catches all outgoing mail so you can test emails locally at `mailpit.test`.

> 🇧🇩 প্রতিটি সাইটের PHP ভার্সন আলাদাভাবে বদলানো যায়; ডিফল্ট সেট করুন **Settings**-এ।
> **Services** পেজ থেকে আলাদা সার্ভিস চালু/বন্ধ করা যায়। **Mailpit** আপনার পাঠানো সব মেইল ধরে রাখে
> (টেস্টের জন্য) — `mailpit.test`-এ দেখা যায়।

### 9. Networking: share & route · নেটওয়ার্কিং (শেয়ার ও রাউটিং)

- **Share publicly**: right-click a site → **Share** to get a temporary public **`https://…`** link via Cloudflare Tunnel — no router/port-forwarding setup. Great for showing clients.
- **Networking** page → **Apply routing**:
  - **LAN sharing** — let other devices on your Wi-Fi open your sites (binds to `0.0.0.0`).
  - **HTTP/2 (QUIC)** — enables HTTP/2 over HTTPS.
  - **Custom TLD** — change `.test` to anything you like.
  - **Port forwarding** — expose sites on an extra port.

> 🇧🇩 **Share** চাপলে Cloudflare Tunnel দিয়ে সাময়িক একটা পাবলিক **`https://…`** লিংক পাবেন —
> ক্লায়েন্টকে দেখানোর জন্য দারুণ। **Networking** পেজে **Apply routing** চেপে চালু করতে পারবেন:
> **LAN sharing** (একই Wi-Fi-র অন্য ডিভাইস থেকে অ্যাক্সেস), **HTTP/2**, **Custom TLD** (`.test`
> বদলে নিজের পছন্দমতো), ও **Port forwarding**।

### 10. Settings · সেটিংস

- **Settings** lets you set: default PHP, default web server, sites folder, TLD & ports, **start at login**, **keep running in tray**, list page sizes, and **Check for updates** (downloads & runs the newest installer automatically).
- There is a **Bengali UI language (বাংলা)** toggle. ⚠️ *Note:* it saves your preference, but the app's own labels aren't fully translated yet — full Bengali UI is on the roadmap.

> 🇧🇩 **Settings**-এ ডিফল্ট PHP, সার্ভার, ফোল্ডার, TLD/পোর্ট, **লগইনে অটো-স্টার্ট**, **ট্রে-তে চালু রাখা**,
> এবং **Check for updates** (নতুন ভার্সন নিজে নামিয়ে ইনস্টল) পাবেন। একটা **বাংলা ভাষা** টগলও আছে —
> ⚠️ এটা পছন্দ সেভ করে, তবে এখনো অ্যাপের সব লেখা বাংলায় অনুবাদ করা হয়নি (পরিকল্পনায় আছে)।

### 11. Tray & quitting · ট্রে ও বন্ধ করা

- Closing the window **keeps your sites running** in the system tray (right-click the tray icon → **Open** / **Quit**). Turn this off in Settings if you prefer the window-close to quit fully.

> 🇧🇩 উইন্ডো বন্ধ করলেও সাইটগুলো **ট্রে-তে চালু থাকে** (ট্রে আইকনে রাইট-ক্লিক → **Open/Quit**)।
> চাইলে Settings থেকে এটা বন্ধ করে দিতে পারেন।

### ❓ Quick troubleshooting · দ্রুত সমাধান

| Problem | Fix · সমাধান |
|---|---|
| Site won't open (`.test`) | Click **Start all**; accept the **UAC** prompt (hosts file). · **Start all** চাপুন, UAC-তে **Yes** দিন। |
| HTTPS warning in browser | Click **Secure** on the site to issue a cert. · সাইটে **Secure** চাপুন। |
| SmartScreen on install | **More info → Run anyway** (unsigned, expected). · **More info → Run anyway**। |
| Port 80/443 already used | Change ports in **Settings** (e.g. 8080). · **Settings**-এ পোর্ট বদলান। |

---

## Layout

```
windows/
  BanglaHost.sln
  src/BanglaHost.Core/   shared brains (paths, models, php-cgi mgr, engine) — net8.0
  src/BanglaHost.Cli/    banglahost.exe — transparent CLI over Core           — net8.0
  src/BanglaHost.App/    WinUI 3 GUI (unpackaged)                          — net8.0-windows
  installer/          Inno Setup script → BanglaHost-Setup.exe
  build.ps1           dotnet publish + iscc
```

## Prerequisites

- Visual Studio 2022 (17.10+) with **.NET desktop** + **Windows App SDK** workloads,
  or the standalone .NET 8 SDK + Windows App SDK.
- (For the installer) [Inno Setup 6](https://jrsoftware.org/isdl.php).

## Develop

```powershell
git clone https://github.com/mohammad-sheikh-shahinur-rahman/BanglaHost
cd BanglaHost\windows
dotnet build BanglaHost.sln
dotnet run --project src\BanglaHost.Cli -- init      # try the CLI
dotnet run --project src\BanglaHost.Cli -- status
# the GUI (run from Visual Studio for the best XAML/debug experience, or:)
dotnet run --project src\BanglaHost.App
```

### Status

**Phases 1–4 are implemented and working** (CLI `banglahost.exe` + a functional WinUI GUI):

- `init`, `install <nginx|php@8.4|mkcert>` (portable-zip downloads; falls back to a
  local Laragon install on dev boxes), `site add/rm/php`, `start/stop/restart`,
  `enable/disable`, `secure <domain>`, `status`/`api`, `db {list|create|drop}`, `adminer`.
- PHP runs as **`php-cgi.exe` over TCP** (no php-fpm on Windows) — port scheme
  `9100 + maj*10 + min` (8.4→9184); nginx uses `fastcgi_pass 127.0.0.1:<port>`.
- **HTTPS** via mkcert (`secure`) — issues a trusted cert, re-renders the vhost's
  ssl block, reloads nginx.
- **Databases**: BanglaHost runs its own MySQL/MariaDB on `127.0.0.1:3306` (fresh data
  dir under `data\`, passwordless root) + `db create/list/drop`. **Adminer** is a
  one-command DB UI served at `adminer.<tld>`.
- **Mailpit** (`mailpit`): catches outgoing mail (SMTP `:1025`), web UI fronted at
  `mailpit.<tld>`. **Node** (`node list|install|use|uninstall`) via fnm.
- Admin-only steps (hosts file, mkcert CA install) go through
  **`banglahost-elevate.exe`** (requireAdministrator) for a single UAC prompt.
  (CI/automation can set `BANGLAHOST_SKIP_HOSTS=1` to skip the hosts step.)
- **WinUI GUI** (`BanglaHost.App`): Dashboard (live status, start/stop all, log),
  Sites, Services, Settings (autostart). Run it from a **self-contained** build (or
  install the Windows App Runtime 1.6) — see Release below.

Also implemented: **system tray** (close hides to tray, right-click → Open/Quit),
`php ini path|reload`, **`php ioncube <ver>`** (downloads the matching Windows loader
and enables it via a per-version `conf.d`), `php status`, and an **in-app updater**
(Settings → Check for updates → downloads + runs the latest `BanglaHost-Setup.exe`).

### Roadmap / Planned Features

- **PHP & Development Tools**: Xdebug Toggle, Composer/NPM GUI, Log Viewer
- **Language & Runtime Support**: Go/Rust or Docker
- **UI/UX**: full Bengali (বাংলা) interface translation

**Recently shipped (v1.1.0):** Custom TLD, LAN sharing, HTTP/2, and port forwarding —
all from the **Networking** page (**Apply routing**); plus one-click **WordPress/Laravel**
installers and an inline **`.env` editor**.

### Code signing (TODO before public distribution)

The installer + exes are currently **unsigned**, so Windows SmartScreen shows
"Windows protected your PC" → users click **More info → Run anyway** (the analog of
macOS "Open Anyway"). To sign, get an OV/EV code-signing certificate and run, after
`build.ps1`:

```powershell
$st = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\<ver>\x64\signtool.exe"
& $st sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 `
    /f mycert.pfx /p <pwd> `
    installer\dist\BanglaHost-Setup-0.1.0.exe
```

Sign the three payload exes (`BanglaHost.App.exe`, `banglahost.exe`, `banglahost-elevate.exe`)
*before* packaging, then the installer itself. An EV cert clears SmartScreen
immediately; an OV cert builds reputation over time.

## Release

```powershell
.\build.ps1                 # -> windows\installer\dist\BanglaHost-Setup-<ver>.exe
```

Then cut a GitHub release with the `.exe` so the in-app updater (asset matcher:
`.exe`) finds it — same flow as the mac `.pkg`.
