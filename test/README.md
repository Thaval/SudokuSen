# GUT (Godot Unit Test) Setup for SudokuSen

This project uses [GUT](https://github.com/bitwes/gut) for unit and integration testing.

## Installation

1. Open Godot Editor
2. Go to **AssetLib** (top center)
3. Search for "**Gut**"
4. Click **Install** (3 times through the dialogs)
5. Go to **Project → Project Settings → Plugins**
6. Enable the **Gut** plugin

## Test Structure

```
test/
├── unit/                    # Fast unit tests
│   ├── test_sudoku_solver.gd
│   ├── test_sudoku_generator.gd
│   └── test_hint_service.gd
└── integration/             # Slower integration tests
    └── test_game_flow.gd
```

## Running Tests

### From Godot Editor
1. Open the **GUT Panel** at the bottom of the editor
2. Configure test directories: `res://test/unit` and `res://test/integration`
3. Click **Run All** or select specific tests

### From Command Line
```bash
godot --headless -s addons/gut/gut_cmdln.gd -gdir=res://test/unit -gdir=res://test/integration
```

### From VSCode
Install the [GUT VSCode Extension](https://marketplace.visualstudio.com/items?itemName=bitwes.gut-extension)

## Writing Tests

All test scripts must:
- Extend `GutTest`
- Have filenames starting with `test_`
- Have test methods starting with `test_`

### Example Test

```gdscript
extends GutTest

func before_each():
    # Setup before each test
    pass

func after_each():
    # Cleanup after each test
    pass

func test_example():
    assert_eq(1, 1, "1 should equal 1")
    assert_true(true, "true should be true")
```

### Useful Asserts

| Assert | Description |
|--------|-------------|
| `assert_eq(a, b)` | a equals b |
| `assert_ne(a, b)` | a not equals b |
| `assert_true(a)` | a is true |
| `assert_false(a)` | a is false |
| `assert_null(a)` | a is null |
| `assert_not_null(a)` | a is not null |
| `assert_between(a, b, c)` | b <= a <= c |
| `assert_has(array, value)` | array contains value |

### Awaiting in Tests

```gdscript
# Wait for time
await wait_seconds(1.0)

# Wait for signal (with timeout)
await wait_for_signal(obj.my_signal, 3.0)

# Wait for physics frames
await wait_physics_frames(5)
```

### Input Simulation

```gdscript
var sender = InputSender.new(Input)

# Simulate key press
sender.action_down("ui_accept")
await wait_frames(2)
sender.action_up("ui_accept")

# Simulate mouse click
sender.mouse_click(Vector2(100, 100))
```

## Documentation

- [GUT Documentation](https://gut.readthedocs.io/en/latest/)
- [Quick Start Guide](https://gut.readthedocs.io/en/latest/Quick-Start.html)
- [Creating Tests](https://gut.readthedocs.io/en/latest/Creating-Tests.html)
- [Mocking Input](https://gut.readthedocs.io/en/latest/Mocking-Input.html)
- [Input Sender Reference](https://gut.readthedocs.io/en/latest/class_ref/class_gutinputsender.html)
