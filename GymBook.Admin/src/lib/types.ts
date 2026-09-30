// The API's admin contracts (GymBook.Api/Admin/AdminContracts.cs), as the JSON arrives: camelCase, dates as ISO
// strings, weights in kg.

export type Role = "Admin" | "SuperAdmin";
export type AccountStatus = "Active" | "Locked" | "Disabled";

export type Me = { userId: string; email: string; role: Role };

export type DayCount = { date: string; count: number };
export type TopUser = { id: string; email: string; count: number };

export type UserRow = {
  id: string;
  email: string;
  name?: string;
  createdAt: string;
  emailVerified: boolean;
  hasGoogle: boolean;
  role?: Role;
  status: AccountStatus;
  workouts: number;
  plans: number;
  lastActiveAt?: string;
};

export type Dashboard = {
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
  unit: "Kg" | "Lbs";
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

export type UserDetail = {
  user: UserRow;
  hasPassword: boolean;
  activeSessions: number;
  profile?: ProfileInfo;
  stats: TrainingStats;
  plans: PlanRow[];
  recentWorkouts: WorkoutRow[];
  ai: { plan: QuotaUse; chat: QuotaUse; plans30d: number; chats30d: number };
};

export type AiUsage = {
  days: number;
  plans: number;
  chats: number;
  users: number;
  planQuota: { limit: number; period: string };
  chatQuota: { limit: number; period: string };
  byDay: { date: string; plans: number; chats: number }[];
  topUsers: { id: string; email: string; plans: number; chats: number }[];
};
