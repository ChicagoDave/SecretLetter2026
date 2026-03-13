# Jack Toresal and The Secret Letter — OpenSilver Migration

## Project Overview

**Jack Toresal and The Secret Letter** is a commercial interactive fiction game originally published by Textfyre, Inc. in 2009. It was built as a Silverlight 3 application with a custom "book" UI designed by Thomas Lynge, running a Glulx virtual machine (FyreVM) in C# to execute an Inform 7 story file.

Microsoft Silverlight was deprecated in 2021 and is no longer supported by any modern browser. This project migrates the original Silverlight UI to [OpenSilver](https://opensilver.net), an open-source WebAssembly-based reimplementation of Silverlight, to preserve the game in a playable form.

**Goal:** Preservation — faithful reproduction of the original experience in a modern browser, not a rewrite.

---

## Original Architecture

The original solution (`FyreSL.sln`, Visual Studio 2008) contained nine projects. Only three are needed for Secret Letter:

| Project | Role | Description |
|---------|------|-------------|
| **Textfyre.VM** | Glulx Engine | C# implementation of the Glulx virtual machine. Pure computation, no UI. Reads `.ulx` story files and produces output on named channels. |
| **Textfyre.UI** | Shared UI Framework | The "book" interface: page-flipping, story rendering, input handling, map display, hints, save/restore dialogs, word definitions, user settings. |
| **SecretLetter** | Game Shell | Game-specific configuration: App.xaml entry point, StoryHandle overrides, intro book pages, map controls, all game assets (story file, illustrations, fonts, XML configs). |

**Dependency chain:** `SecretLetter → Textfyre.UI → Textfyre.VM`

### Excluded Projects

| Project | Reason |
|---------|--------|
| Nightfall | Different game (unfinished) |
| Shadow | Different game (*The Shadow in the Cathedral*) |
| Textfyre.UI.Transcript.Popup | Standalone transcript viewer — not needed |
| Textfyre.UI.Transcript.Lib | Support library for transcript viewer |
| EncryptGame | Build-time utility for encrypting game files |
| SecretLetterDesktopSetup | Windows installer project |
| TextfyreSLAppWeb | ASP.NET host website |

---

## Codebase Summary

| Metric | Count |
|--------|-------|
| C# source files (3 projects) | ~80 |
| XAML files | ~45 |
| Total C# lines | ~28,000 |
| Total XAML lines | ~5,300 |
| Game assets (images, fonts, XML) | ~15 MB |

### Key Source Files

**Textfyre.VM (the engine)**
- `Engine.cs` — Main Glulx VM execution loop (~59K)
- `Opcodes.cs` — Glulx opcode implementations (~50K)
- `Output.cs` / `OutputBuffer.cs` — Channel-based output system
- `Quetzal.cs` — Save file format (Quetzal standard)
- `UlxImage.cs` — Story file loader
- `Veneer.cs` — Glulx veneer routines

**Textfyre.UI (the book interface)**
- `Controls/TextfyreBook.xaml.cs` — Main book control with page management (~27K)
- `Controls/FlipBook/UCPage.xaml.cs` — Page-turn animation with curl math (~30K + 15K compute)
- `Controls/Input.xaml.cs` — Player text input
- `Controls/StoryScroller.xaml.cs` — Story text scrolling
- `Controls/Mapping/Map.xaml.cs` — Dynamic game map
- `DocSystem/` — Document layout and rendering system
- `Entities/` — Data models (Document, GameState, SaveFile, Map structures)
- `Pages/Story.xaml.cs` — **Core game loop**: loads the VM, manages engine thread, dispatches output to UI
- `Storage/StorageHandler.cs` — Isolated storage wrapper for save games

**SecretLetter (game-specific)**
- `App.xaml` — Application entry point and resource definitions (~110K of XAML resources)
- `StoryHandle.cs` — Game-specific input parsing and TOC handling
- `GameFiles/sl-v1.07e.ulx` — The compiled Inform 7 story file
- `Images/` — Book cover, page art (Erika Swanson illustrations), cameo portraits, intro pages
- `GameFiles/*.xml` — Settings, map data, manual, fonts, word definitions, art references

---

## Migration Risk Assessment

### Low Risk (~70% of codebase) — Should port with minimal changes

- **Textfyre.VM** — Pure C# computation with no UI dependencies. References `System.Windows.Browser` but likely only superficially. The Glulx engine, opcodes, output system, save format, and image loader should compile against OpenSilver/.NET with trivial adjustments.
- **Standard XAML controls** — Grid, StackPanel, Canvas, TextBlock, TextBox, ScrollViewer, Border, Image, UserControl. These are OpenSilver's strongest area.
- **Data binding and resources** — Standard Silverlight patterns.
- **Entity/model classes** — Document, GameState, MapLocation, etc. Pure C# data structures.
- **7Zip compression library** — Embedded LZMA implementation, pure C#.

### Medium Risk — Known workarounds exist

| Area | Files Affected | Issue | Approach |
|------|---------------|-------|----------|
| **IsolatedStorage** | `StorageHandler.cs`, `SaveFile.cs`, `Story.xaml.cs` | OpenSilver supports IsolatedStorage but API surface may differ | Test and adapt; may need browser localStorage fallback |
| **Storyboard animations** | `UCPage.xaml.cs`, `Art.xaml.cs`, `BookCover.xaml.cs`, `StoryScroller.xaml.cs`, `Wait.xaml.cs`, and others | Page-curl animation uses DoubleAnimation with computed clipping paths | OpenSilver supports Storyboards; test rendering fidelity of the complex page-turn math |
| **Resource loading** | `App.xaml` (~110K), various `*.resx` files | Embedded resources and XAML resource dictionaries | OpenSilver handles this but verify binary resource (`.ulx` file) loading |
| **Custom font embedding** | `SecretLetter/Fonts/`, font XML configs | Silverlight font embedding vs. web fonts | May need to convert to web font loading |

### Higher Risk — Requires architectural changes

| Area | Files Affected | Issue | Approach |
|------|---------------|-------|----------|
| **Engine threading model** | `Pages/Story.xaml.cs` | The Glulx VM runs on a `Thread` with `AutoResetEvent` blocking (`inputReadyEvent.WaitOne()`) to synchronize with the UI via `Dispatcher.BeginInvoke`. WebAssembly is single-threaded; blocking waits are not viable. | **Refactor to async/await.** The engine's `LineWanted`/`KeyWanted` events need to become async, yielding control back to the browser while waiting for input. This is the critical-path migration item. |
| **System.Windows.Browser** | ~10 files | `HtmlPage.Window.Alert()`, browser cookie access, HTML element interop | Replace with OpenSilver's JS interop (`Interop.ExecuteJavaScript`) or remove where not essential |
| **WCF Service Reference (FyreService)** | `Service References/` in both SecretLetter and Textfyre.UI | WCF client for logging commands/notes to a Textfyre server (long defunct) | **Remove entirely.** Stub out any calls. The service no longer exists and is not needed for preservation. |

---

## Migration Steps

### Phase 1: Project Setup (Visual Studio)

1. Install the OpenSilver VS extension from the marketplace
2. Create a new OpenSilver solution with:
   - `SecretLetter.OpenSilver` — OpenSilver Application
   - `SecretLetter.OpenSilver.Browser` — (auto-generated Blazor entry point)
   - `SecretLetter.OpenSilver.Simulator` — (auto-generated for testing)
   - `Textfyre.UI.OpenSilver` — OpenSilver Class Library
   - `Textfyre.VM.OpenSilver` — OpenSilver Class Library
3. Set project references: SecretLetter → Textfyre.UI → Textfyre.VM

### Phase 2: Source Migration (Claude Code)

1. Copy all `.cs` and `.xaml` source files from legacy projects into the new OpenSilver project directories
2. Copy all game assets (images, fonts, game files, XML configs)
3. Delete all `Service References/` directories and `ServiceReferences.ClientConfig` files
4. Remove WCF-related references (`System.ServiceModel`, `System.Runtime.Serialization`)
5. Add `#if OPENSILVER` / `#if !OPENSILVER` compiler directives where code diverges
6. Attempt compilation and begin fixing errors

### Phase 3: API Compatibility Fixes (Claude Code)

1. Replace `System.Windows.Browser.HtmlPage` calls with OpenSilver JS interop or remove
2. Adapt `IsolatedStorage` usage if needed
3. Verify `Storyboard` animations compile and render
4. Verify resource/asset loading (especially the `.ulx` binary game file)
5. Fix any remaining namespace or API differences

### Phase 4: Engine Async Refactor (Claude Code + VS testing)

1. Refactor `Story.xaml.cs` to replace `Thread` + `AutoResetEvent` with async/await
2. Modify `Engine.cs` event model to support async input callbacks
3. Ensure `Dispatcher.BeginInvoke` calls translate correctly (OpenSilver supports this but verify behavior)
4. Test the full input→engine→output→render cycle

### Phase 5: Testing and Polish (VS Simulator + Browser)

1. Run in OpenSilver Simulator to verify UI rendering
2. Test page-flipping animation fidelity
3. Test save/restore cycle
4. Test all game channels: main text, location, chapter, time, hints, map, art, conversation topics
5. Verify all illustrations and cameo art display correctly
6. Play through the game to verify end-to-end functionality

---

## FyreVM Channel Architecture

The Glulx engine communicates with the UI through named output channels. Understanding these is essential for debugging:

| Channel | Purpose |
|---------|---------|
| `Main` | Primary story text (rendered as formatted "FyreXML") |
| `Location` | Current room name (displayed in page headers) |
| `Chapter` | Current chapter name (displayed in page headers) |
| `Time` | Turn counter |
| `Title` | Story title |
| `Credits` | Author/credits text |
| `Prompt` | Input prompt character (typically `>`) |
| `Theme` | Art theme ID (triggers page art display) |
| `Hints` | Hint tree data |
| `Map` | Map location/connection updates |
| `Conversation` | NPC conversation topics |
| `WordDef` | Highlighted word definitions |
| `Sound` | Sound effect triggers |

---

## File Inventory

```
SecretLetter/
├── App.xaml / App.xaml.cs          — Application entry, resource dictionaries
├── Page.xaml / Page.xaml.cs        — Root page
├── StoryHandle.cs                  — Game-specific VM bridge
├── Controls/
│   ├── IntroBook.xaml/.cs          — Illustrated intro sequence
│   ├── MapLeft.xaml/.cs            — Left-page map display
│   └── MapRight.xaml/.cs           — Right-page map display
├── GameFiles/
│   ├── sl-v1.07e.ulx              — Compiled Inform 7 story
│   ├── Settings.xml                — Game configuration
│   ├── Map.xml                     — Map data
│   ├── Manual.xml                  — In-game manual
│   ├── Fonts.xml                   — Font configuration
│   ├── Arts.xml                    — Art reference definitions
│   └── WordDefinition.xml          — In-game glossary
├── Images/
│   ├── BookCover.jpg, TitlePage.jpg, prologue.jpg
│   ├── openbook.jpg, leftpage.jpg, rightpage.jpg
│   ├── MapLeft.jpg, MapRight.jpg
│   ├── PageArt/                    — Scene illustrations (Erika Swanson)
│   ├── CameoArt/                   — Character portraits
│   ├── Intro/                      — Intro book page images (p1-p8)
│   └── BookmarkTOC.xaml            — Bookmark vector graphic
└── Fonts/                          — Embedded typefaces

Textfyre.UI/
├── Master.xaml/.cs                 — Master layout
├── Page.xaml/.cs                   — UI root page
├── Settings.cs                     — Application settings/constants
├── UserSettings.cs                 — User preferences (fonts, colors)
├── SpotArt.cs                      — Spot art management
├── Keyboard.cs                     — Keyboard input handler
├── GameModes.cs                    — Game state enum
├── Controls/
│   ├── TextfyreBook.xaml/.cs       — Main book control
│   ├── TextfyreBookPage.xaml/.cs   — Individual book page
│   ├── Input.xaml/.cs              — Text input control
│   ├── FyreDocument.xaml/.cs       — Document display
│   ├── StoryScroller.xaml/.cs      — Scrolling story text
│   ├── StoryAid.xaml/.cs           — Story aid panel
│   ├── Art.xaml/.cs                — Art display
│   ├── BookCover.xaml/.cs          — Book cover animation
│   ├── Bookmark.xaml/.cs           — Bookmark tab
│   ├── More.xaml/.cs               — "More" indicator
│   ├── FlipButton.xaml/.cs         — Page flip button
│   ├── TextButton.xaml/.cs         — Styled text button
│   ├── TableOfContent.xaml/.cs     — Table of contents
│   ├── Topic.xaml/.cs              — Conversation topics
│   ├── Wait.xaml/.cs               — Loading spinner
│   ├── FlipBook/                   — Page-turning animation system
│   ├── Hints/                      — Hint display system
│   ├── IODialog/                   — Save/Restore/Transcript dialogs
│   ├── Mapping/                    — Dynamic map control
│   ├── Manual/                     — In-game manual viewer
│   ├── SpeechBubble/               — NPC speech bubbles
│   ├── UserSettings/               — Font/color picker controls
│   └── Demographic/                — User demographics form
├── Current/                        — Global state accessors
│   ├── Application.cs, Game.cs, User.cs, Font.cs, Platform.cs
├── DocSystem/                      — Document layout engine
├── Entities/                       — Data models
├── Helpers/                        — Color utilities, mouse click manager
├── Storage/                        — Isolated storage wrapper
├── Pages/
│   └── Story.xaml/.cs              — Core game loop (ENGINE THREAD)
└── 7Zip/                           — LZMA compression for save files

Textfyre.VM/
├── Engine.cs                       — Glulx VM main loop
├── Opcodes.cs                      — Opcode implementations
├── Output.cs                       — Output channel definitions
├── OutputBuffer.cs                 — Output buffering
├── Veneer.cs                       — Veneer function handling
├── UlxImage.cs                     — Story file loader
├── Quetzal.cs                      — Save file format
├── HeapAllocator.cs                — Memory heap management
├── BigEndian.cs                    — Byte order utilities
└── VMException.cs                  — VM exception type
```

---

## Credits

- **Story:** Michael Gentry (writing), with contributions from Jacqueline Ashwell (testing lead), Graeme Jefferis, and others
- **Inform 7 extensions:** Textfyre custom extensions (FyreVM Support, Standard Rules, Quips, Scripted Events, etc.)
- **UI Design:** Thomas Lynge (Deluxe Edition book interface)
- **Illustrations:** Erika Swanson
- **FyreVM Engine:** Based on Demeter (C# Glulx), developed by Textfyre
- **Publisher:** Textfyre, Inc. (Geneva, IL, 2007–2014), David Cornelson founder
- **OpenSilver Migration:** 2026, preservation project

---

## License

The original game content, artwork, and story are © Textfyre, Inc. The Textfyre.VM engine includes a separate COPYRIGHT file. OpenSilver is MIT-licensed. This migration is undertaken for preservation purposes.