# WinAerials Project Blueprint & Rules

<Role>
You are an expert .NET 10 Developer building a lightweight Windows desktop automation utility.
</Role>

<Constraints>
- Language: C# 14
- Runtime: .NET 10 File-Based App Model (Single-File Assembly)
- Layout: Strict SINGLE-FILE format (`WinAerials.cs`)
- External Dependencies: Allowed ONLY if Native AOT / Source-Generator friendly.
- Core Directives required at top of file:
    #:property TargetFramework net10.0
    #:property PublishAot true
    #:property PublishSingleFile true
    #:package VYaml
</Constraints>

<Logic>
1. Configuration Architecture & Smart Paths:
   - **Path Resolution:** The application must dynamically resolve its working directory using a "Local-First" strategy:
     1. Check if `config.yaml` exists in the current application execution directory (`./config.yaml`). If found, lock this folder as the working directory for both config and `playlist.js`.
     2. If not found locally, default the working path securely to the user's roaming profile: `$env:APPDATA/WinAerials/`.
   - If `config.yaml` is missing in both locations, initialize the `$env:APPDATA/WinAerials/` directory and generate it explicitly using the string literal structure provided in <DefaultYaml>.
   - **AOT Binding Implementation:** Use `VYaml.Annotations` attributes (`[YamlObject]`) on a `partial class` schema representation of the configuration map. The tool must parse the configuration cleanly via `YamlSerializer.Deserialize<AppConfig>(...)` with zero runtime reflection.

2. State Engine & Persistent Queue:
   - Store historical sequence tracking items in `$env:APPDATA/WinAerials/playlist.js`.
   - If empty/missing, read the target endpoint from the configuration object's `AppleConfigUrl` property.
   - Extract the 4K asset array mapping raw stream URLs to their companion 'accessibilityLabel' values.
   - Shuffle via an in-place Fisher-Yates algorithm using `Random.Shared`.
   - On execution, pop the top entry off, update `playlist.js`, and store the current video data.

3. Template Composition & Layout Layering:
   - Generate a local `index.html` file inside `$env:APPDATA/WinAerials/`.
   - The webpage must render the active `.mov` clip full-bleed (`object-fit: cover; width: 100vw; height: 100vh; position: absolute;`).
   - **Typography Engine:** If `font_url` inside the config object is NOT empty, inject a standard HTML `<link rel="stylesheet" href="...">` tag into the document header. Apply the string provided in `font_name` directly as the default root CSS `font-family` variable across the webpage.
   - Linearly loop over the array collection mapped out in the configuration's `TextOverlays` property list.
   - For each text item, generate an independent fixed DOM `<div>` overlay. Map the textual instructions from the configuration properties, dynamically evaluating macros before writing the HTML code.
   - Look up macro strings and replace them programmatically inside the C# engine:
     - `%location%` -> Populated via the Apple Video metadata structural text.
     - `%time%` -> Injects an inline native HTML/JS real-time engine hook running matching standard user formatting specifications.
   - Map placement alignment keys to absolute viewport positions inside the generated document styles using the coordinate rules specified in <LayoutCSSRules>.

4. Intelligent Target Resolution Strategy:
   - Parse `LivelyPath` from the configuration class. Log and track every resolution step to format a clear traceback message string if validation checks fail:
     - **Explicit Override:** If a string path value is assigned, check its validity.
     - **Intelligent Default:** If empty, query running processes for an active `Lively` background instance (`MainModule.FileName`). If missing, check local app data (`%localappdata%/Programs/Lively Wallpaper/Lively.exe`), followed by global system paths (`C:\Program Files (x86)\Lively Wallpaper\Lively.exe`).
   - If found, trigger the desktop update via `System.Diagnostics.Process` using arguments: `setwp --file "[path/to/index.html]"`.
   - **Target Missing Failure:** If all resolution lookups fail, bubble an explicit `FileNotFoundException` tracking the exact searched target checkpoints.

5. Global Fatal Exception Interception & UI Popups:
   - Wrap the main program loop inside an all-encompassing `try-catch` block to handle unexpected application failures (network request timeouts, layout errors, validation checks).
   - If an error hits the root block, use P/Invoke or native forms components to display a crisp Win32 dialog popup wrapper showcasing details of the crash.
   - **Lively Resolution Error Formatting:** If the crash is caused by the application being unable to resolve the `Lively.exe` pathway, format the dialog prompt message cleanly:
     - Display a headline detailing that Lively Wallpaper could not be found.
     - Output the sequential verification trace list highlighting exactly what mechanics were tried (Manual path check, running process scanning, standard installer paths).
     - Append a helpful CTA recommending installing the application directly from the Microsoft Store as the easiest resolution path.
     - **Interactive Repair Protocol:** Trigger a secondary shell command to open the default system web/store router directly to the application package page using the native UWP deep link: `ms-windows-store://pdp/?ProductId=9NTM2QC6QWS7`

6. Native Edge-Case Resilience:
   - HTML Autoplay Guard: The generated HTML video tag must explicitly include 'autoplay muted playsinline loop' attributes, paired with a DOM window.onload JS override calling element.play() to guarantee video initialization under Chromium/WebView2 restrictions.
   - CSS Reset: Ensure html/body layout overrides completely strip padding, margins, and overflow rules, assigning a pitch-black background-color to mitigate display flashes during transitions.
   - Network Backoff: Implement a 3-tier 2/4/8 second exponential retry loop for the remote configuration web requests to survive initialization delays during system startup. but if network is completely unavilable after retries, just have that be cleanly fatal with a clear message box indication.
</Logic>

<DefaultYaml>
# WinAerials Configuration File

# Explicit path targeting Lively.exe. Leave completely blank "" to use dynamic auto-detection.
lively_exe_path: ""

# Source configuration registry for Apple's screensaver ecosystem
aerials_source_url: "https://configuration.apple.com/configurations/internetservices/aerials/resources-config.plist"

# Global Typography Preferences (Supports web fonts like Google Fonts)
# Leave font_url completely blank "" to fall back onto native system sans-serif rules
font_name: "Inter"
font_url: "https://fonts.googleapis.com/css2?family=Inter:wght@300;400;700&display=swap"

# Collection array handling text layouts rendered over your wallpaper stream.
# Valid placements: top, bottom, middle, left, right, center (joined via hyphen, e.g., top-center)
text_overlays:
  - content: "%time%"
    time_format: "hh:mm A" # Valid variables: hh (12hr), HH (24hr), mm (minutes), A (AM/PM marker)
    placement: "bottom-left"
    font_size: "2.5rem"
    font_weight: "300"
    opacity: 0.9

  - content: "%location%"
    placement: "bottom-left"
    font_size: "1.1rem"
    font_weight: "400"
    opacity: 0.7
</DefaultYaml>

<LayoutCSSRules>
Translate text placement choices into standardized absolute CSS layout block parameters using this exact coordinate structure:
- "left": left: 40px; text-align: left;
- "right": right: 40px; text-align: right;
- "center": left: 50%; transform: translateX(-50%); text-align: center;
- "top": top: 40px;
- "bottom": bottom: 40px;
- "middle": top: 50%; transform: translateY(-50%);
* Note: If coordinate sets overlap or use multiple transform parameters (e.g., middle-center), unify properties cleanly into a single unified transform block style statement (e.g., `transform: translate(-50%, -50%);`).
</LayoutCSSRules>