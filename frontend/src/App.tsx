import { useState } from "react";
import { Dashboard } from "./components/Dashboard";
import { LoginForm } from "./components/LoginForm";
import { Logo } from "./components/Logo";
import { RegisterForm } from "./components/RegisterForm";
import { ThemeToggle } from "./components/ThemeToggle";
import { useAuth } from "./context/AuthContext";

export function App() {
  const { isAuthenticated } = useAuth();
  const [showRegister, setShowRegister] = useState(false);

  if (isAuthenticated) {
    return <Dashboard />;
  }

  return (
    <div className="auth-page">
      <div className="theme-toggle-corner">
        <ThemeToggle />
      </div>
      <div className="auth-page-inner">
        <div className="brand">
          <Logo />
          <div className="brand-text">
            <span className="brand-title">JobTrail</span>
            <span className="brand-tagline">Track every application, in one place</span>
          </div>
        </div>

        {showRegister ? (
          <RegisterForm onSwitchToLogin={() => setShowRegister(false)} />
        ) : (
          <LoginForm onSwitchToRegister={() => setShowRegister(true)} />
        )}
      </div>
    </div>
  );
}
