# Sprinkler AutoConnect

A Revit API add-in designed to automate fire sprinkler connections to MEP fire protection piping systems.

The tool detects unconnected sprinklers, identifies suitable fire protection pipes, and automatically creates the required branch routing, fittings, and connections while maintaining proper pipe geometry.

## Workflow

```text
Unconnected Sprinkler
        ↓
Find Suitable Fire Protection Pipe
        ↓
Analyze Pipe Connector & Connection Point
        ↓
Create Branch Pipe
        ↓
Split Main Pipe (If Required)
        ↓
Create Tee / Elbow Fittings
        ↓
Create Vertical Drop to Sprinkler
        ↓
Connect Sprinkler
```

## Features

```text
✓ Detect unconnected sprinklers
✓ Find suitable fire protection pipes
✓ Connector-based geometry analysis
✓ Automatic branch pipe creation
✓ Smart routing logic (horizontal branch + vertical drop)
✓ Main pipe splitting and Tee fitting creation
✓ Automatic elbow fitting creation
✓ Batch sprinkler processing
✓ Individual rollback for failed connections
✓ Failed sprinkler selection for manual review
✓ Revit transaction-safe operations
```

## Technologies

```text
C#
.NET
Autodesk Revit API
Revit MEP API
WPF / MVVM
CommunityToolkit.Mvvm
Nice3point Revit Toolkit
LINQ
```

## Architecture

The add-in follows a service-based architecture separating Revit operations into dedicated components:

```text
UI (WPF / MVVM)
        ↓
External Events
        ↓
Connection Services
        ↓
Revit API Operations
        ↓
MEP Elements & Connectors
```

## Current Status

Core Revit API automation is implemented, including sprinkler detection, pipe analysis, branch creation, fitting generation, and connection handling.

The project is currently being enhanced with more advanced routing rules and MEP design validation logic.

## Future Improvements

```text
• Advanced pipe routing optimization
• Support for more complex sprinkler layouts
• Automatic pipe sizing based on design rules
• Enhanced error reporting and diagnostics
• Additional fire protection system automation tools
```
