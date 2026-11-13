export interface AssignmentReminder {
  recommendationId: number; title: string; titleAr?: string; content: string; contentAr?: string;
  enabled: boolean; intervalMinutes: number; soundEnabled: boolean; nextDueAt?: string | null; completedToday: boolean;
}
export interface ReminderDelivery {
  recommendationId: number; astronautId: number; token: string; title: string; titleAr?: string;
  content: string; contentAr?: string; sentAt: string; isManual: boolean; soundEnabled: boolean; senderName?: string;
}
export interface AssignmentReminders { assignments: AssignmentReminder[]; pending: ReminderDelivery[] }
