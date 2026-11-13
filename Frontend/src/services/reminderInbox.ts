import { ReminderDelivery } from '../types/reminders';

export function mergeReminderInbox(current: ReminderDelivery[], incoming: ReminderDelivery[], astronautId: number): ReminderDelivery[] {
  const result = [...current];
  for (const reminder of incoming) {
    if (reminder.astronautId !== astronautId || !reminder.token) continue;
    const index = result.findIndex(item => item.recommendationId === reminder.recommendationId);
    if (index < 0) result.push(reminder);
    else if (result[index].token !== reminder.token && Date.parse(reminder.sentAt) >= Date.parse(result[index].sentAt)) result[index] = reminder;
  }
  return result;
}

export function intervalInMinutes(value: number, unit: 'minutes' | 'hours'): number {
  if (!Number.isInteger(value) || value < 1) throw new Error('Choose a whole reminder interval.');
  const minutes = unit === 'hours' ? value * 60 : value;
  if (minutes > 10080) throw new Error('The maximum reminder interval is seven days.');
  return minutes;
}
