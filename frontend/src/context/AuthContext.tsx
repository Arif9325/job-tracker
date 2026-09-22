import { createContext, useContext, useState, type ReactNode } from "react";

interface AuthContextValue {
  email: string | null;
  isAuthenticated: boolean;
  login: (token: string, email: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  // Initialize from localStorage so a page refresh doesn't log you out.
  const [email, setEmail] = useState<string | null>(() =>
    localStorage.getItem("email")
  );

  function login(token: string, userEmail: string) {
    localStorage.setItem("token", token);
    localStorage.setItem("email", userEmail);
    setEmail(userEmail);
  }

  function logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("email");
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
