import { useState, type FormEvent } from "react";
import { authApi } from "../api";
import { useAuth } from "../context/AuthContext";

export function RegisterForm({ onSwitchToLogin }: { onSwitchToLogin: () => void }) {
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (password.length < 8) {
      setError("Password must be at least 8 characters.");
      return;
    }

    setLoading(true);
    try {
      const result = await authApi.register(email, password);
      login(result.token, result.email);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Registration failed");
    } finally {
      setLoading(false);
    }
  }

  return (
    <form className="auth-form" onSubmit={handleSubmit}>
      <h2>Create an account</h2>
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
          minLength={8}
          required
        />
      </label>
      <button type="submit" disabled={loading}>
        {loading ? "Creating account..." : "Register"}
      </button>
      <p className="switch-link">
        Already have an account?{" "}
        <button type="button" className="link-button" onClick={onSwitchToLogin}>
          Log in
        </button>
      </p>
    </form>
  );
}
