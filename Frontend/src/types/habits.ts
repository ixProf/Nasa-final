export interface HabitDay { date: string; count: number; points: number }
export interface HabitAssignment { id: number; title: string; titleAr?: string; content: string; contentAr?: string; reason: string; reasonAr?: string; points: number; completedToday: boolean }
export interface HabitDashboard { assignments: HabitAssignment[]; heatmap: HabitDay[]; totalPoints: number; currentStreak: number; longestStreak: number; totalCompletions: number; level: number; badges: string[]; timeZone: string }
