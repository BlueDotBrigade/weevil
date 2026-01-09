# Weevil: User Interface Style Guide

- [Color Palette](#color-palette)
	- [Image Size](#image-size)
- [Icons](#icons)
	- [General](#general)
	- [Icons Having State](#icons-having-state)
- [Appendices](#appendices)
	- [Appendix S: Icon Source](#appendix-s-icon-source)
		- [Filter Results Window](#filter-results-window)
		- [Dashboard](#dashboard)

## Color Palette

- Dialog box
	- left side: #3c3c3c (dark grey)
- Dashboard
	- Insight
		- border + title bar: #3c3c3c (dark grey)

### Image Size

| Type       | DPI | Dimensions | Comments                                                            |
| ---------- | --- | ---------- | ------------------------------------------------------------------- |
| Status Bar | 96  | 24x24      | Icons have 20% padding within the image canvas. See: Icons8 Padding |
| Insight    | 96  | 48x48      |                                                                     |


## Icons

### General

- The file name should end with a post fix which represents the icon's dimensions
	- For example: `-24.png` is used for a 24x24 pixel icon.

### Icon State

No data available:

- sample file name: `Insight-NoData-24.png`
- Color: #3c3c3c (dark grey)

Has Data:

- sample file name: `Insight-Data-24.png`
- Color: #949494 (light grey)

Has Data Requiring Attention

- sample file name: `Insight-DataRequiresAttention-24.png`
- Color: #900A22 (burgundy)

---

## Appendices

### Appendix C: Color Palette

- Dark Mode Backgrounds → #1A1A1A, #2C2C2C, #3C3C3C
- Text on Dark UI → #C0C0C0 (for contrast)
- Borders & Dividers → #6B6B6B, #7F7F7F
- Disabled States → #949494, #A9A9A9
- Light Mode Backgrounds → #EEEEEE, #FAFAFA, #FFFFFF
- Hover Effects → Slightly lighter or darker versions of primary elements

Grayscale Palette:

- #1A1A1A – Very Dark Gray (near-black, great for deep backgrounds)
- #2C2C2C – Dark Gray (UI elements, dark mode backgrounds)
- #3C3C3C – Charcoal Gray (Yes, good for text on dark UI, subtle borders)
- #525252 – Medium-Dark Gray (Hover states, secondary text on dark UI)
- #6B6B6B – Muted Gray (Dividers, secondary elements)
- #7F7F7F – Neutral Gray (Inactive icons, disabled buttons)
- #949494 – Medium-Light Gray (Yes, good for placeholder text, soft backgrounds)
- #A9A9A9 – Soft Gray (UI separators, subtle accents)
- #C0C0C0 – Light Gray (Form backgrounds, low emphasis text)
- #D6D6D6 – Lighter Gray (Input fields, light theme borders)
- #E0E0E0 – Very Light Gray (Disabled controls, minimal contrast elements)
- #EEEEEE – Almost White (Alternative background)
- #F5F5F5 – Ultra Light Gray (Background shading)
- #FAFAFA – Near White (Softest UI elements)
- #FFFFFF – Pure White (High contrast backgrounds)

### Appendix S: Icon Source

Many of the Weevil icons have been sourced from [Icons8.com][Icons8] which allows content to be used free of charge provided that:

- > macOS and Windows applications should have the link to https://icons8.com in the about section.

#### Filter Results Window

| Usage                    | Source                                                             | Notes                                            |
| ------------------------ | ------------------------------------------------------------------ | ------------------------------------------------ |
| Open Documents           | Windows Metro →Popular → Documents and Folders→ Document           |                                                  |
| Time Elapsed             | Windows Metro →Science → Lab Measurements→ Time                    |                                                  |
| Context                  | iOS Filled →Editing → Text Editing→ Table                          | padding: -10%                                    |
| Records: Visible         | Windows 10 →Messaging → User Status→ Eye                           |                                                  |
| Records: Selected        | iOS Filled →Editing → Image Editing→ Chisel Tip Marker             |                                                  |
| Records: On Disk         | iOS Glyph →User Interface → Basic Elements→ Save                   |                                                  |
| Information              | iOS Filled →User Interface → Symbols→ Information                  | Circle 88%, Icon 60%, Circle #949494, I: #3c3c3c |
| Warning                  | IFluent System Filled →Messaging → Message Status→ High Importance |                                                  |
| Error                    | Material Rounded →Popular → Status→ Cancel                         |                                                  |
| Insight                  | Fluent System Filled →Household → Lighting→ Light On               |                                                  |
| New Release Notification | iOS Filled →Messaging→ Megaphone                                   |                                                  |
| Document Level Remarks   | Windows Metro → Editing → Image Editing → Pencil                   |                                                  |

#### Dashboard

| Usage             | Source                                                   | Notes                  |
| ----------------- | -------------------------------------------------------- | ---------------------- |
| Generic Insight   | Ice Cream →Data → Charts→ Combo Chart                    |                        |
| Critical Errors   | Ice Cream →Industry → Warning Signs→ Radioactive         |                        |
| UI Responsiveness | iOS Filled →Programming → Coding→ Application Window     | Add hourglass overlay. |
| Selected Range    | Fluent System Filled →Time And Date → Dates→ Calendar 31 | no padding             |
| Time Gap          | iOS Filled →Time And Date → Clocks→ Clock    |

[Icons8]: https://icons8.com/icons/
