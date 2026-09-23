import { createContext, useContext, useState, type ReactNode } from "react";
import { clearAuth, getEmail, saveAuth } from "../storage";

interface AuthContextValue {
  email: string | null;
  isAuthenticated: boolean;
  login: (token: string, email: string, remember: boolean) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  // Initialize from storage (checks localStorage, then sessionStorage)
  // so a page refresh doesn't log you out either way.
  const [email, setEmail] = useState<string | null>(() => getEmail());

  function login(token: string, userEmail: string, remember: boolean) {
    saveAuth(token, userEmail, remember);
    setEmail(userEmail);
  }

  function logout() {
    clearAuth();
    setEmail(null);
  }

  return (
    <AuthContext.Provider
      value={{ email, isAuthenticated: email !== null, login, logout }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used inside an AuthProvider");
  }
  return ctx;
}
