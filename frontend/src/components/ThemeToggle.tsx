import { useTheme } from "../context/ThemeContext";

export function ThemeToggle() {
  const { theme, toggleTheme } = useTheme();

  return (
    <button
      type="button"
      className="theme-toggle secondary"
      onClick={toggleTheme}
      aria-label="Toggle light/dark theme"
      title="Toggle light/dark theme"
    >
      {theme === "dark" ? "☀️" : "🌙"}
    </button>
  );
}
