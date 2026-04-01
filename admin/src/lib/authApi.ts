const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5044";

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  userId: number;
  username: string;
  role: string;
  isActive: boolean;
}

export interface MeResponse {
  userId: number;
  username: string;
  role: string;
}

export const authApi = {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    const res = await fetch(`${API_BASE}/Auth/login`, {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(credentials),
    });

    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: "Login failed" }));
      throw new Error(err.message ?? "Login failed");
    }

    return res.json() as Promise<LoginResponse>;
  },

  async getMe(): Promise<MeResponse> {
    const res = await fetch(`${API_BASE}/Auth/me`, {
      credentials: "include",
    });

    if (!res.ok) {
      throw new Error("Unauthorized");
    }

    return res.json() as Promise<MeResponse>;
  },

  async logout(): Promise<void> {
    await fetch(`${API_BASE}/Auth/logout`, {
      method: "POST",
      credentials: "include",
    });
  },
};
