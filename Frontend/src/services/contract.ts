// Accept the numeric enums used by the hosted API and string enums from newer deployments.
const fields: Record<string, string[]> = {
  severity: ['', 'Routine', 'MedicalAttention', 'Emergency'],
  overallStatus: ['', 'Routine', 'MedicalAttention', 'Emergency'],
  bodyPosition: ['', 'MicrogravityFloating', 'Sitting', 'Supine', 'Standing'],
  activityContext: ['', 'Resting', 'DuringExercise', 'PostExercise', 'PreSleep', 'PostSleep', 'ExtravehicularActivity'],
  mealContext: ['', 'Fasting', 'PostMeal', 'Random'],
  dataQuality: ['', 'Valid', 'Uncertain', 'Invalid'],
  quality: ['', 'Poor', 'Fair', 'Good', 'Excellent'],
  category: ['', 'Medical', 'Physical', 'Psychological', 'Sleep', 'Environmental'],
  priority: ['', 'Low', 'Medium', 'High', 'Urgent'],
};
function statusValues(value: Record<string, unknown>, endpoint: string): string[] {
  if ('triggeredAt' in value || 'severity' in value || endpoint.startsWith('/alerts')) return ['', 'Active', 'Acknowledged', 'Resolved'];
  if ('generatedAt' in value || endpoint.startsWith('/recommendations')) return ['', 'Pending', 'Acknowledged', 'Completed', 'Dismissed'];
  if ('destination' in value || endpoint.startsWith('/missions')) return ['', 'Scheduled', 'Active', 'Completed', 'Aborted'];
  if (endpoint === '/sync' && 'itemType' in value) return ['Accepted','Duplicate','Rejected'];
  return ['', 'Routine', 'MedicalAttention', 'Emergency'];
}
export function convertContract(value: unknown, endpoint: string, outbound = false): any {
  if (Array.isArray(value)) return value.map(item => convertContract(item, endpoint, outbound));
  if (!value || typeof value !== 'object') return value;
  const obj = value as Record<string, unknown>;
  return Object.fromEntries(Object.entries(obj).map(([key, item]) => {
    const names = key === 'status' ? statusValues(obj, endpoint) : fields[key];
    if (names && typeof item === 'number' && !outbound) return [key, names[item] || item];
    if (names && typeof item === 'string' && outbound && names.includes(item)) return [key, names.indexOf(item)];
    return [key, convertContract(item, endpoint, outbound)];
  }));
}
