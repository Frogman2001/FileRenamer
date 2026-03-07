# File Renamer

A small Windows desktop app for **curating your images**: browse, cull, and rename files in a folder. The program is intentionally simple—use a real editing program to edit your photos.

## What it does

- **Pick a folder** – Point it at a folder of files (e.g. photos).
- **Browse & preview** – List all files, preview JPG/JPEG in the viewer, step through with Previous/Next or the mouse wheel.
- **Cull** – Check files you don’t want and delete them in bulk, or delete the current file.
- **Rename** – Apply simple rules to filenames:
  - **Filter** – Only include files whose names contain given text.
  - **Prepend** – Add text at the start of each name.
  - **Append** – Add text before the extension.
  - **Replace** – Find and replace text in names.
- **Log** – After renaming, a JSON log is saved in the same folder so you can see what was changed.

## Not for

- **Editing images** – No cropping, filters, or adjustments. Use your usual image editor for that.
- **Heavy batch workflows** – It’s a straightforward tool for organizing and naming, not a full asset pipeline.

## Requirements

- Windows
- .NET 6.0

## Building & running

```bash
dotnet build
dotnet run
```

Or open the solution in Visual Studio and run from there.
