# Gemstone Disk Operating System (GST-DOS)

GST-DOS is a lightweight, DOS-like operating system designed to provide a nostalgic yet functional command-line environment. Developed as an experimental project, GST-DOS handles basic file management, directory navigation, and kernel configuration. Also, I recently found out that I named the project "GST-DOS 0.11" and not "GST-DOS". I don't know how to change this, so this is unfortunate.

## Features
- **Core Kernel:** Manages memory and basic system operations.
- **Command Shell:** Intuitive interface with support for standard commands.
- **Virtual Disk Configuration:** Allows users to set up system properties and manage virtual sectors.
- **Memory Management:** Includes High Memory Area (HMA) support for legacy applications.

## Getting Started
To install GST-DOS, run the provided setup utility. The process involves:
1. **User Registration:** Set up your system ownership profile.
2. **System Options:** Configure kernel memory settings (e.g., enabling HMA).
3. **File Installation:** The setup will write the core shell binaries and boot scripts to your target directory.

## Commands
Once in the shell (`C:\>`), you can use the following commands:
- `HELP`: Display this list of commands.
- `VER`: Show system version and license information.
- `DIR`: List directory contents.
- `MD`: Create a new directory.
- `CD`: Change the current directory.
- `TYPE`: View the contents of a file.
- `DEL`: Remove a target file.
- `MEM`: Display memory mapping information.
- `SETUP`: Re-run the installation wizard.
- `EXIT`: Close the application.

## Troubleshooting

If you encounter a "Gem Screen of Death" (GSOD), the system may have encountered a serialization error during configuration. Please check the logs (if available) to identify any null references or data mismatches in your configuration files.

## Credits
- **Creators:** Developed by SpideyBash

## Tip
**ONLY FOR WINDOWS!** (i think)
For the best experience, go to your terminal, and type Ctrl+,
Then, click the 3 bars, also known as the hamburger menu.
Click Defaults
Additional settings > Appearance
Color scheme to Vintage
Font face to a DOS font or similar
Retro terminal effects
AND YOU'RE DONE!

---
*GST-DOS © 2026 Gemstone Corp.*
