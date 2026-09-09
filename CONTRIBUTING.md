# Contributing to Tango

Thank you for your interest in contributing to **Tango**! We welcome contributions from the community.

Please take a moment to read this guide and our [Code of Conduct](./CODE_OF_CONDUCT.md) to help ensure a smooth and productive collaboration for everyone.

---

## Contribution Policy

To keep our repository organized, **all contributions must be linked to an issue**. 

Before starting any work:
1. Check existing [Issues](https://github.com/Tayra-Sakurai/Tango/issues) to see if a relevant topic already exists.
2. If you find an issue you would like to work on, leave a comment stating your intention to resolve it.
3. If no matching issue exists, **please open a new issue first** before submitting code.

---

## Reporting Issues & Feature Requests

Feel free to report bugs or request new features on the [Issues](https://github.com/Tayra-Sakurai/Tango/issues) page. Please search existing issues (both open and closed) before opening a new one to avoid duplicates.

---

## Development & Contribution Workflow

### Step 1: Identify or Open an Issue
Find an existing issue on the [Issues](https://github.com/Tayra-Sakurai/Tango/issues) page or create a new one outlining your proposed changes. Review any discussion or comments on the issue to plan your implementation.

### Step 2: Fork & Branch
1. Fork the repository to your account.
2. Create a new topic branch for your changes.
3. **AI Agent Branch Naming Convention:** If you are using an AI development tool (such as Codex, Claude Code, or Antigravity), prefix your branch name with the agent name (e.g., `antigravity/fix-window-padding` or `claude-code/add-settings-page`).

### Step 3: Code Guidelines & Testing
* **Line Endings:** Use Windows line endings (`CRLF`) since this is a Windows 11 application built with the Windows App SDK and WinUI.
* **Testing:** Run all existing unit tests and ensure your changes pass before submitting. Add new tests where applicable.

### Step 4: Submit a Pull Request
1. Double-check that your work is linked to a valid issue. If necessary, create a final issue to ensure full traceability.
2. Open a Pull Request (PR) against the `master` branch.
3. Fill out the PR template completely, referencing the issue number (e.g., `Fixes #123` or `Closes #456`).