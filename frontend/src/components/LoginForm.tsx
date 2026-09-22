import { useState, type FormEvent } from "react";
import { authApi } from "../api";
import { useAuth } from "../context/AuthContext";

export function LoginForm({ onSwitchToRegister }: { onSwitchToRegister: () => void }) {
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const result = await authApi.login(email, password);
      login(result.token, result.email);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form className="auth-form" onSubmit={handleSubmit}>
      <h2>Log in</h2>
      {error && <p className="error">{error}</p>}
      <label>
        Email
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
      </label>
      <label>
        Password
        <input
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
      </label>
      <button type="submit" disabled={loading}>
        {loading ? "Logging in..." : "Log in"}
      </button>
      <p className="switch-link">
        No account?{" "}
        <button type="button" className="link-button" onClick={onSwitchToRegister}>
          Register
        </button>
      </p>
    </form>
  );
}
