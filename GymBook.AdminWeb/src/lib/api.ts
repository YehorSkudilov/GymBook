// The web admin's side of the API's /api/admin endpoints: the same calls and rules as the admin app
// (GymBook.Admin/Src/Services/AdminApi.cs). The API checks every permission; the page only follows the same rules so
// it doesn't offer what would be refused. The API's address is baked in at build time (NEXT_PUBLIC_API_URL), and the
// API must allow this site's origin (Cors__AllowedOrigins).

export const apiUrl = (process.env.NEXT_PUBLIC_API_URL || "").replace(/\/+$/, "");

export const AdminRole = "Admin";
export const SuperAdminRole = "SuperAdmin";

/** The end of Pro given for good (ProGrant.Forever on the server). */
export const ProForever = "9999-12-31T00:00:00+00:00";
export const isForever = (until: string) => new Date(until).getUTCFullYear() >= 9999;

export type UserRow = {
  id: string;
  email: string;
  name?: string;
  createdAt: string;
  emailVerified: boolean;
  hasGoogle: boolean;
  role?: string;
  status: string;
  workouts: number;
  plans: number;
  lastActiveAt?: string;
  pro: boolean;
};

export type DayCount = { date: string; count: number };
export type TopUser = { id: string; email: string; count: number };

export type DashboardResponse = {
  totalUsers: number;
  verifiedUsers: number;
  newUsers7d: number;
  newUsers30d: number;
  activeUsers7d: number;
  disabledUsers: number;
  admins: number;
  workoutsTotal: number;
  workouts7d: number;
  plansTotal: number;
  aiPlans7d: number;
  aiChats7d: number;
  signupsByDay: DayCount[];
  workoutsByDay: DayCount[];
  recentUsers: UserRow[];
  mostActive7d: TopUser[];
};

export type ProfileInfo = {
  name: string;
  goal: string;
  experience: string;
  daysPerWeek: number;
  sessionMinutes: number;
  equipmentAccess: string;
  unit: string;
  bodyWeightKg: number;
  birthYear?: number;
  bodyFatPercent?: number;
  trainingSince?: string;
  onboardingDone: boolean;
  updatedAt: string;
};

export type TrainingStats = {
  workouts: number;
  workouts30d: number;
  workingSets: number;
  volumeKg: number;
  totalHours: number;
  firstWorkoutAt?: string;
  lastWorkoutAt?: string;
  customExercises: number;
  bodyWeightEntries: number;
  latestBodyWeightKg?: number;
  workoutsByWeek: { weekStart: string; count: number }[];
};

export type PlanRow = {
  id: string;
  name: string;
  goal: string;
  daysPerWeek: number;
  days: number;
  exercises: number;
  isActive: boolean;
  createdAt: string;
};

export type WorkoutRow = {
  id: string;
  name: string;
  startedAt: string;
  endedAt?: string;
  exercises: number;
  workingSets: number;
  volumeKg: number;
  planWeek?: number;
};

export type QuotaUse = { used: number; limit: number; period: string };

export type ProInfo = {
  grantedUntil?: string;
  store?: "GooglePlay" | "AppStore" | "MicrosoftStore";
  productId?: string;
  storeExpiresAt?: string;
  autoRenewing: boolean;
  inTrial: boolean;
};

export type UserDetail = {
  user: UserRow;
  hasPassword: boolean;
  activeSessions: number;
  profile?: ProfileInfo;
  stats: TrainingStats;
  plans: PlanRow[];
  recentWorkouts: WorkoutRow[];
  ai: { plan: QuotaUse; chat: QuotaUse; plans30d: number; chats30d: number };
  pro: ProInfo;
};

export type AiUsageResponse = {
  days: number;
  plans: number;
  chats: number;
  users: number;
  planQuota: { limit: number; period: string };
  chatQuota: { limit: number; period: string };
  byDay: { date: string; plans: number; chats: number }[];
  topUsers: { id: string; email: string; plans: number; chats: number }[];
};

export type RankReportRow = {
  id: string;
  reporter: string;
  targetUserId: string;
  targetUsername: string;
  targetEmail: string;
  lift?: string;
  reason: string;
  createdAt: string;
  targetHidden: boolean;
  openAboutTarget: number;
};

export type AdminSettings = { proRequired: boolean };

type AuthResponse = {
  userId: string;
  email: string;
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  emailVerified: boolean;
};

/** The API refused the request; the message is safe to show. */
export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}

/** The session can't be renewed, or the account lost its admin role: sign in again. */
export class SessionExpiredError extends Error {
  constructor(message = "Your session has expired. Please sign in again.") {
    super(message);
  }
}

export type Session = {
  userId: string;
  email: string;
  role: string;
  refreshToken: string;
  accessToken?: string;
  accessTokenExpiresAt?: number;
};

const storageKey = "gymbook.admin.session";

/** The "role" claim of an access token: Admin, SuperAdmin, or null for everyone else. */
function roleOf(accessToken: string): string | null {
  try {
    const part = accessToken.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const role = JSON.parse(atob(part.padEnd(part.length + ((4 - (part.length % 4)) % 4), "="))).role;
    return role === AdminRole || role === SuperAdminRole ? role : null;
  } catch {
    return null;
  }
}

// Only the refresh token is kept between visits; access tokens live in memory.
function load(): Session | null {
  try {
    const saved = localStorage.getItem(storageKey);
    return saved ? (JSON.parse(saved) as Session) : null;
  } catch {
    return null;
  }
}

function save(session: Session | null) {
  try {
    if (session) {
      const { userId, email, role, refreshToken } = session;
      localStorage.setItem(storageKey, JSON.stringify({ userId, email, role, refreshToken }));
    } else localStorage.removeItem(storageKey);
  } catch {
    // Storage blocked (private window): the session lasts until the tab closes.
  }
}

async function problem(response: Response): Promise<ApiError> {
  let message: string | undefined;
  try {
    const body = await response.json();
    const errors = body?.errors ? (Object.values(body.errors) as string[][]).flat() : [];
    message = errors[0] ?? body?.title;
  } catch {
    // Not a problem-details body (e.g. a proxy error page).
  }
  message ??=
    response.status === 429
      ? "Too many attempts. Please wait a minute and try again."
      : response.status === 403
        ? "You don't have permission to do that."
        : "Something went wrong. Please try again.";
  return new ApiError(response.status, message);
}

async function post(path: string, body: unknown): Promise<Response> {
  return fetch(`${apiUrl}/${path}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
}

export class AdminApi {
  session: Session | null = typeof window === "undefined" ? null : load();
  private refreshing: Promise<string> | null = null;
  /** Called when the session was cleared and the page should go back to sign-in. */
  onExpired?: (message: string) => void;

  get isSuperAdmin() {
    return this.session?.role === SuperAdminRole;
  }

  /** Signs in; refuses (and revokes the new session of) an account that isn't an admin. */
  async signIn(email: string, password: string) {
    const response = await post("api/auth/login", { email, password });
    if (!response.ok) throw await problem(response);
    const auth = (await response.json()) as AuthResponse;
    const role = roleOf(auth.accessToken);
    if (!role) {
      await this.revoke(auth.refreshToken);
      throw new ApiError(403, "This account doesn't have admin access.");
    }
    if (!auth.emailVerified) {
      await this.revoke(auth.refreshToken);
      throw new ApiError(403, "Verify this account's email in the Gym Book app first.");
    }
    this.set(auth, role);
  }

  async signOut() {
    if (this.session) await this.revoke(this.session.refreshToken);
    this.clear();
  }

  /** Gets a fresh access token, e.g. on opening the page to find out whether the saved session still works. */
  ensureSignedIn() {
    return this.accessToken(true);
  }

  dashboard = () => this.get<DashboardResponse>("api/admin/dashboard");

  users(query: string, filter: string | null) {
    const params = new URLSearchParams();
    if (query.trim()) params.set("q", query.trim());
    if (filter) params.set("filter", filter);
    const qs = params.toString();
    return this.get<UserRow[]>("api/admin/users" + (qs ? `?${qs}` : ""));
  }

  user = (id: string) => this.get<UserDetail>(`api/admin/users/${encodeURIComponent(id)}`);
  aiUsage = (days: number) => this.get<AiUsageResponse>(`api/admin/ai-usage?days=${days}`);
  settings = () => this.get<AdminSettings>("api/admin/settings");
  /** SuperAdmin only. */
  setSettings = (settings: AdminSettings) => this.send("PUT", "api/admin/settings", settings);

  setDisabled = (id: string, disabled: boolean) => this.send("POST", userPath(id, "disabled"), { disabled });
  setEmailVerified = (id: string, verified: boolean) => this.send("POST", userPath(id, "email-verified"), { verified });
  signOutEverywhere = (id: string) => this.send("POST", userPath(id, "sign-out"));
  resetAiQuota = (id: string) => this.send("POST", userPath(id, "ai-quota/reset"));
  sendPasswordReset = (id: string) => this.send("POST", userPath(id, "password-reset-email"));
  /** Gym Book Pro until a date (ProForever for good), or taken back (null). Allowed on your own account too. */
  setProGrant = (id: string, until: string | null) => this.send("POST", userPath(id, "pro"), { until });
  /** SuperAdmin only. Null makes the account a regular user. */
  setRole = (id: string, role: string | null) => this.send("POST", userPath(id, "role"), { role });
  /** SuperAdmin only. Deletes the account and all its data. */
  deleteUser = (id: string) => this.send("DELETE", `api/admin/users/${encodeURIComponent(id)}`);

  rankReports = (all: boolean) => this.get<RankReportRow[]>(`api/admin/ranks/reports?all=${all}`);
  /** Closes every open report about the reported user. */
  resolveReport = (id: string) => this.send("POST", `api/admin/ranks/reports/${id}/resolve`);
  setRankHidden = (userId: string, hidden: boolean) =>
    this.send("POST", `api/admin/ranks/users/${encodeURIComponent(userId)}/hidden`, { hidden });
  /** Clears the user's public bio and picture. */
  clearPublicProfile = (userId: string) => this.send("POST", `api/admin/ranks/users/${encodeURIComponent(userId)}/clear-profile`);

  private async get<T>(path: string): Promise<T> {
    const response = await this.withToken("GET", path);
    return (await response.json()) as T;
  }

  private async send(method: string, path: string, body?: unknown) {
    await this.withToken(method, path, body);
  }

  private async withToken(method: string, path: string, body?: unknown): Promise<Response> {
    const request = (token: string) =>
      fetch(`${apiUrl}/${path}`, {
        method,
        headers: { Authorization: `Bearer ${token}`, ...(body === undefined ? {} : { "Content-Type": "application/json" }) },
        body: body === undefined ? undefined : JSON.stringify(body),
      });
    let response = await request(await this.accessToken(false));
    if (response.status === 401) {
      // Rejected (expired early, revoked): renew once and retry.
      response = await request(await this.accessToken(true));
      if (response.status === 401) this.expire();
    }
    if (!response.ok) throw await problem(response);
    return response;
  }

  private accessToken(forceRefresh: boolean): Promise<string> {
    const s = this.session;
    if (!s) return Promise.reject(new SessionExpiredError());
    if (!forceRefresh && s.accessToken && (s.accessTokenExpiresAt ?? 0) > Date.now() + 60_000) return Promise.resolve(s.accessToken);
    // One refresh at a time: a refresh token works once.
    this.refreshing ??= this.refresh(s.refreshToken).finally(() => (this.refreshing = null));
    return this.refreshing;
  }

  private async refresh(refreshToken: string): Promise<string> {
    const response = await post("api/auth/refresh", { refreshToken });
    if (response.status === 401 || response.status === 400) this.expire();
    if (!response.ok) throw await problem(response);
    const auth = (await response.json()) as AuthResponse;
    // The role is read from the database at every refresh, so a removed role shows up here.
    const role = roleOf(auth.accessToken);
    if (!role) {
      await this.revoke(auth.refreshToken);
      this.expire("This account no longer has admin access.");
    }
    this.set(auth, role!);
    return auth.accessToken;
  }

  private set(auth: AuthResponse, role: string) {
    this.session = {
      userId: auth.userId,
      email: auth.email,
      role,
      refreshToken: auth.refreshToken,
      accessToken: auth.accessToken,
      accessTokenExpiresAt: Date.parse(auth.accessTokenExpiresAt),
    };
    save(this.session);
  }

  private clear() {
    this.session = null;
    save(null);
  }

  private expire(message?: string): never {
    this.clear();
    const error = new SessionExpiredError(message);
    this.onExpired?.(error.message);
    throw error;
  }

  private async revoke(refreshToken: string) {
    try {
      await post("api/auth/logout", { refreshToken });
    } catch {
      // Best effort.
    }
  }
}

const userPath = (id: string, action: string) => `api/admin/users/${encodeURIComponent(id)}/${action}`;
