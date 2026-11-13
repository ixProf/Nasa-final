import type { HealthAlert } from '../types/telemetry';

export function isAssessmentAlert(alert: HealthAlert): boolean {
  return (alert.title === 'Red-Flag Symptom Escalation' && alert.reason === 'Automated escalation triggered by symptom guidance assessment detecting red-flag clinical signs.') ||
    (alert.title === 'Critical Assessment Finding' && alert.reason === 'A matching symptom pattern has a related recent emergency vital reading.');
}

export function applyResolvedAlert(alerts: HealthAlert[], resolved: HealthAlert): HealthAlert[] {
  return alerts.map(alert => {
    if (alert.id === resolved.id) return resolved;
    if (isAssessmentAlert(resolved) && alert.status !== 'Resolved' &&
        alert.astronautId === resolved.astronautId && alert.title === resolved.title &&
        alert.message === resolved.message && alert.reason === resolved.reason && alert.severity === resolved.severity)
      return { ...alert, status: resolved.status, acknowledgedAt: alert.acknowledgedAt || resolved.acknowledgedAt,
        acknowledgedBy: alert.acknowledgedBy || resolved.acknowledgedBy, actionTaken: resolved.actionTaken };
    return alert;
  });
}

export function openAlertMessages(alerts: HealthAlert[]): string[] {
  const open = alerts.filter(alert => alert.status !== 'Resolved');
  const priority = { Emergency: 2, MedicalAttention: 1, Routine: 0 };
  return [...new Set([...open].sort((a, b) => priority[b.severity] - priority[a.severity])
    .map(alert => (isAssessmentAlert(alert) ? alert.message : alert.reason || alert.message).trim())
    .filter(Boolean))];
}
