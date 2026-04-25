---
trigger: always_on
---

# Project Rules
## core Philosophy
> **"Readability and Order > Speed and Complex Interconnections"**


This project prioritizes maintainability and clarity over clever, hyper-optimized code. Even if a solution is slightly less performant but significantly easier to read, choose the readable one (within reason).

- **SOLID Principles**: Follow Single Responsibility, Open-Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion principles for maintainable and extensible code.
- **DRY (Don't Repeat Yourself)**: Avoid code duplication by extracting common logic into reusable functions, classes, or modules.
- **KISS (Keep It Simple, Stupid)**: Strive for simplicity in design and implementation. Avoid over-engineering.
- **Clean Code**: Write readable, self-documenting code with meaningful names, small functions, and clear structure.
- **Error Handling**: Implement robust error handling and logging to aid debugging and maintain reliability. Use low-cardinality logging with stable message strings e.g. `logger.info{id, foo}, 'Msg'`, `logger.error({error}, 'Another msg')`, etc



## 1. Code Logic & Structure
- **Explicit over Implicit**: Avoid "magic" one-liners. Use descriptive variable names.
- **Component Structure**:
  - Keep components small and focused.
  - Every component should have a clear purpose.
- **Comments**:
  - Explain *why*, not just *what* in spanis only.
  - Document complex logic block by block.

## 3. CSS & Styling
- **Vanilla CSS / CSS Modules**:
  - Use separate CSS files or modules for components to keep styles organized.
  - Use CSS Variables (`:root`) for global themes (colors, spacing, etc.).
- **Avoid**:
  - Inline styles (except for dynamic values).
  - Tailwind CSS (unless explicitly requested later).

## 4. Workflows
- Always verify changes locally before asking for review.
- Follow the defined workflows in `.agent/workflows/`.# Project Rules