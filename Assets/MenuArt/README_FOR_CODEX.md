# One More Night — Unity Menu Art Pack

This folder is meant to be copied directly into your Unity project here:

```text
Assets/MenuArt/
```

Recommended final project path:

```text
<YourUnityProject>/Assets/MenuArt/
```

## What is inside

```text
MenuArt/
  Backgrounds/   Fullscreen menu/settings backgrounds and modal overlay
  Buttons/       Blank reusable button sprites, no baked text
  Panels/        Parchment panels and value boxes
  Tabs/          Blank settings tab states
  Sliders/       Slider track/fill/handles
  Toggles/       Checkbox/toggle states and checkmark
  Dropdowns/     Choice boxes, arrows, dropdown list and option states
  Scrollbars/    Scroll view backgrounds, tracks, handles
  Keybinds/      Keybind and reset button states
  Decorations/   Logo/title banners, side banners, gems, frame pieces, ornaments
  Icons/         Category/action icons
```

## Important import notes for Unity

For every transparent PNG UI asset except fullscreen backgrounds:

```text
Texture Type: Sprite (2D and UI)
Alpha Source: Input Texture Alpha
Alpha Is Transparency: ON
Compression: None or High Quality
Filter Mode: Bilinear
```

For large panels/buttons meant to stretch:

```text
Sprite Mode: Single
Mesh Type: Full Rect
Set borders in Sprite Editor for 9-slicing
Image Type in UI: Sliced
```

The files marked as backgrounds should stay as regular full-screen images/sprites:

```text
Backgrounds/background_main_forest.png
Backgrounds/background_settings_forest.png
```

## How Codex should use these assets

Ask Codex to build the UI using TextMeshPro text on top of these blank PNG controls. Do not use button images with text baked in. Only these title/logo assets contain text:

```text
Decorations/logo_one_more_night.png
Decorations/title_settings_banner.png
```

Main menu suggested hierarchy:

```text
Canvas
  Background Image -> MenuArt/Backgrounds/background_main_forest.png
  MainMenuRoot
    Logo -> MenuArt/Decorations/logo_one_more_night.png
    Left Banner -> MenuArt/Decorations/banner_side_left.png
    Right Banner -> MenuArt/Decorations/banner_side_right.png
    Button Column
      New Game Button -> MenuArt/Buttons/button_large_normal.png + TMP text "New Game"
      Continue Button -> MenuArt/Buttons/button_large_active.png + TMP text "Continue"
      Load Game Button -> MenuArt/Buttons/button_large_normal.png + TMP text "Load Game"
      Settings Button -> MenuArt/Buttons/button_large_normal.png + TMP text "Settings"
      Exit Button -> MenuArt/Buttons/button_large_normal.png + TMP text "Exit"
    Credits Button -> MenuArt/Buttons/button_footer_normal.png + MenuArt/Icons/icon_book_credits.png + TMP text "Credits"
```

Settings menu suggested hierarchy:

```text
Canvas
  Background Image -> MenuArt/Backgrounds/background_settings_forest.png
  SettingsRoot
    Title -> MenuArt/Decorations/title_settings_banner.png
    Left Banner -> MenuArt/Decorations/banner_side_left.png
    Right Banner -> MenuArt/Decorations/banner_side_right.png
    Tabs Row
      Display -> MenuArt/Tabs/tab_active.png + TMP text "Display"
      Keybind -> MenuArt/Tabs/tab_normal.png + TMP text "Keybind"
      Audio -> MenuArt/Tabs/tab_normal.png + TMP text "Audio"
      Graphics -> MenuArt/Tabs/tab_normal.png + TMP text "Graphics"
    Panel -> MenuArt/Panels/panel_settings_background.png, Image Type Sliced
      Resolution Row -> panel_row_background + choice_box_normal
      Fullscreen Row -> panel_row_background + toggle_box_on_normal + toggle_checkmark
      VSync Row -> panel_row_background + toggle_box_on_normal + toggle_checkmark
      Brightness Row -> panel_row_background + slider_track + slider_fill + slider_handle_normal
      UI Scale Row -> panel_row_background + slider_track + slider_fill + slider_handle_normal
    Back Button -> MenuArt/Buttons/button_footer_normal.png + TMP text "Back"
    Apply Button -> MenuArt/Buttons/button_apply_normal.png + TMP text "Apply"
```

## Button state mapping

Use these Unity button sprite states:

```text
Normal      *_normal.png
Highlighted *_hover.png
Pressed     *_pressed.png
Disabled    *_disabled.png
Selected    *_active.png where available
```

Examples:

```text
Buttons/button_large_normal.png
Buttons/button_large_hover.png
Buttons/button_large_pressed.png
Buttons/button_large_disabled.png
Buttons/button_large_active.png
```

## Suggested Codex prompt

```text
Build a Unity UGUI main menu and settings menu using assets from Assets/MenuArt. Use TextMeshPro for all button/tab/setting text. Do not use baked text except Decorations/logo_one_more_night.png and Decorations/title_settings_banner.png. Use sliced Image components for panels and buttons. Create MainMenuController and SettingsMenuController scripts. Main menu buttons: New Game, Continue, Load Game, Settings, Exit, Credits. Settings tabs: Display, Keybind, Audio, Graphics. Display tab contains Resolution dropdown, Fullscreen toggle, VSync toggle, Brightness slider, UI Scale slider, Back, Apply. Use the matching sprite state files for normal/highlighted/pressed/disabled/active states.
```

## Notes

Most UI assets are transparent PNGs. The only non-transparent/fullscreen images are the two forest background images. `modal_backdrop.png` is intentionally a semi-transparent dark overlay.
