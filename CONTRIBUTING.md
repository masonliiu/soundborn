# Contributing to Soundborn

Thank you for your interest in contributing to Soundborn! This document provides guidelines and instructions for contributing.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Commit Message Guidelines](#commit-message-guidelines)
- [Pull Request Process](#pull-request-process)
- [Testing Guidelines](#testing-guidelines)
- [Documentation](#documentation)

## Code of Conduct

- Be respectful and inclusive
- Provide constructive feedback
- Help others learn and grow
- Focus on what's best for the project

## Getting Started

### Prerequisites

- Unity 6.2 (6000.2.15f1)
- Git
- Basic knowledge of C# and Unity

### Setting Up Development Environment

1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/your-username/Soundborn.git
   cd Soundborn
   ```
3. Open the project in Unity
4. Verify all packages are installed (Unity will prompt if needed)

### Finding Work

- Check the [Issues](https://github.com/masonliiu/Soundborn/issues) tab for bugs and feature requests
- Look for issues tagged `good first issue` if you're new
- Ask before working on major features to avoid duplicate work

## Development Workflow

### Branch Strategy

- **main:** Stable, production-ready code
- **develop:** Integration branch for features (if applicable)
- **feature/**: New features
- **bugfix/**: Bug fixes
- **hotfix/**: Critical fixes

### Creating a Branch

```bash
# create and checkout a new branch
git checkout -b feature/your-feature-name

# or for bug fixes
git checkout -b bugfix/issue-number-description
```

### Making Changes

1. Make your code changes
2. Test thoroughly in Unity Editor
3. Ensure no compiler errors or warnings
4. Test the feature/bug fix in isolation
5. Test integration with existing systems

### Before Committing

- [ ] Code compiles without errors
- [ ] No Unity console warnings (or justified warnings)
- [ ] Feature works as intended
- [ ] No breaking changes (or documented)
- [ ] Code follows style guidelines
- [ ] Added comments for complex logic

## Coding Standards

### C# Style Guide

**Naming Conventions:**
```csharp
// classes: PascalCase
public class BattleController { }

// methods: PascalCase
public void ProcessNextTurn() { }

// private fields: camelCase
private CharacterStats currentActor;

// public fields/properties: PascalCase
public CharacterStats[] enemyActors;

// constants: PascalCase
public const int MAX_PARTY_SIZE = 4;

// local variables: camelCase
var targetEnemy = enemyMembers[index];
```

**Code Organization:**
```csharp
// 1. Fields (public first, then private)
// 2. Properties
// 3. Unity Lifecycle Methods (Awake, Start, Update, etc.)
// 4. Public Methods
// 5. Private Methods
// 6. Coroutines
// 7. Event Handlers
```

**Comments:**
```csharp
// use XML comments for public methods
/// <summary>
/// processes the next turn in the battle sequence.
/// </summary>
/// <param name="actor">the character whose turn it is</param>
public void ProcessNextTurn(CharacterStats actor) { }

// use inline comments for complex logic
// calculate element advantage multiplier
float multiplier = CalculateElementMultiplier(attacker.element, defender.element);
```

### Unity-Specific Guidelines

**Inspector Serialization:**
- Use `[Header("Section Name")]` to organize inspector fields
- Use `[Tooltip("Description")]` for complex fields
- Use `[Range(min, max)]` for numeric sliders

**Performance:**
- Cache frequently accessed components
- Use object pooling for frequently instantiated objects
- Avoid `FindObjectOfType` in Update() methods
- Use coroutines for asynchronous operations

**Error Handling:**
- Use `Debug.Log()` for informational messages
- Use `Debug.LogWarning()` for potential issues
- Use `Debug.LogError()` for actual errors
- Add null checks before using Unity objects

## Commit Message Guidelines

Use clear, descriptive commit messages:

```
Type: Brief description (50 chars max)

Longer explanation if needed. Explain what and why, not how.
- Bullet points for multiple changes
- Reference issue numbers: Fixes #123
```

**Types:**
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `style:` Code style changes (formatting, etc.)
- `refactor:` Code refactoring
- `perf:` Performance improvements
- `test:` Adding tests
- `chore:` Maintenance tasks

**Examples:**
```
feat: Add multi-enemy targeting system

Implement tap-to-target enemy selection with rotating indicators.
Auto-selects lowest HP enemy when ability is pressed.

fix: Resolve HP bar not updating for enemy slot 2

The enemyHpSliders array wasn't being updated correctly for
enemies beyond the first slot. Fixed UpdateEnemyMemberUI().

docs: Update README with battle system details

Add comprehensive documentation for multi-enemy combat,
targeting system, and technical architecture.
```

## Pull Request Process

### Before Submitting

1. **Update Documentation:**
   - Update README if adding features
   - Add code comments for new public methods
   - Update inline documentation if needed

2. **Test Your Changes:**
   - Test in Unity Editor
   - Test edge cases
   - Verify no regressions

3. **Clean Up:**
   - Remove debug logs (or use `[Conditional("UNITY_EDITOR")]`)
   - Remove commented-out code
   - Remove unused variables

### Creating a Pull Request

1. Push your branch to your fork:
   ```bash
   git push origin feature/your-feature-name
   ```

2. Open a Pull Request on GitHub:
   - Use a clear, descriptive title
   - Fill out the PR template (if one exists)
   - Reference related issues: "Closes #123"
   - Add screenshots/GIFs for UI changes

3. PR Description Template:
   ```markdown
   ## Description
   Brief description of changes

   ## Type of Change
   - [ ] Bug fix
   - [ ] New feature
   - [ ] Documentation update
   - [ ] Code refactoring

   ## Testing
   How was this tested?
   - [ ] Tested in Unity Editor
   - [ ] Tested on device
   - [ ] Manual testing checklist

   ## Screenshots (if applicable)
   [Add screenshots/GIFs here]

   ## Checklist
   - [ ] Code follows style guidelines
   - [ ] Self-review completed
   - [ ] Comments added for complex logic
   - [ ] Documentation updated
   - [ ] No new warnings introduced
   ```

### Review Process

- Address feedback promptly
- Don't force-push after review starts (use new commits)
- Be open to suggestions and alternative approaches
- Keep PRs focused (one feature/fix per PR)

## Testing Guidelines

### Manual Testing Checklist

For battle system changes:
- [ ] Turn order works correctly
- [ ] All 4 enemies are tracked
- [ ] HP bars update for all characters
- [ ] Target selection works (tap to select, tap again to attack)
- [ ] Status effects apply and tick correctly
- [ ] Death effects play for all characters
- [ ] Victory condition triggers when all enemies are dead

For UI changes:
- [ ] Works on different screen sizes
- [ ] No UI elements overlap
- [ ] Animations are smooth
- [ ] Text is readable

### Testing Multi-Enemy Battles

The battle system should handle:
- 4 enemies simultaneously
- Individual HP tracking per enemy
- Independent status effects
- Proper turn order with all enemies
- Victory only when all enemies are defeated

## Documentation

### Code Documentation

- Add XML comments for public methods
- Explain "why" not just "what" in complex logic
- Document edge cases and assumptions

### README Updates

If adding features, consider updating:
- Features list
- Usage examples
- Technical documentation section
- Project structure (if adding new files)

## Areas for Contribution

### High Priority

- Bug fixes from the issues tracker
- Performance optimizations
- UI/UX improvements

### Medium Priority

- Code refactoring and cleanup
- Additional unit tests
- Documentation improvements

### Feature Development

Coordinate with maintainers before starting major features:
- Equipment system
- New status effects
- Additional character abilities
- Boss mechanics

## Questions?

- Open an issue with the `question` label
- Check existing documentation first
- Review closed PRs for similar changes

Thank you for contributing to Soundborn! 🎵

