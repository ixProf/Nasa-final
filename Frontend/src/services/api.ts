import { Submissions } from './submissions';
import { OfflineMeasurements, MeasurementEndpoint } from './offlineMeasurements';
import { HabitDashboard } from '../types/habits';
import { AssignmentReminders, ReminderDelivery } from '../types/reminders';
import {
  VitalSignReading,
  HealthAlert,
  SleepRecord,
  MoodEntry,
  Recommendation,
  RecommendationStatus,
  ChatMessage,
  CreateVitalReadingPayload,
} from '../types/telemetry';

import { API_BASE } from './config';
import { reportError } from './errors';
import { convertContract } from './contract';

class ApiService {
  private offlineMeasurements=new OfflineMeasurements(localStorage);
  private measurementOwner(): string | null {
    try {
      const claims=JSON.parse(atob((this.accessToken || '').split('.')[1].replace(/-/g,'+').replace(/_/g,'/')));
      return claims.astronaut_id ? String(claims.astronaut_id) : null;
    } catch {return null;}
  }
  public getOfflineMeasurements() {
    const owner=this.measurementOwner();return owner?this.offlineMeasurements.list(owner):[];
  }
  private async measurement<T>(endpoint: MeasurementEndpoint, options: RequestInit): Promise<T> {
    const owner=this.measurementOwner();
    const payload=JSON.parse(String(options.body));
    try {
      const saved=await this.request<T>(endpoint,options);
      if(owner){
        try {this.offlineMeasurements.remove(owner,payload.clientGeneratedId);window.dispatchEvent(new Event('app:queue-changed'));}
        catch(error){reportError(error);} // A cache problem must not turn a confirmed server save into a failed save.
      }
      return saved;
    } catch(error) {
      if(owner && this.measurementOwner()===owner && (error as Error).name==='TypeError') {
        this.offlineMeasurements.save(owner,endpoint,payload);
        window.dispatchEvent(new Event('app:queue-changed'));
        const queued=Object.assign(new Error(localStorage.getItem('nasa_lang')==='ar'?'حُفظت القراءة على هذا الجهاز وتنتظر المزامنة. لم تُحفظ على الخادم بعد.':'Saved on this device, pending synchronization. Not yet saved on the server.'),{queued:true});
        reportError(queued);throw queued;
      }
      throw error;
    }
  }
  public async syncPendingMeasurements(): Promise<{synced:number;duplicates:number}> {
    return this.submissions.run('offline-sync',()=>this.flushMeasurements());
  }
  private async flushMeasurements(): Promise<{synced:number;duplicates:number}> {
    const owner=this.measurementOwner();if(!owner)throw new Error('Sign in as the astronaut who recorded these measurements.');
    const rows=this.offlineMeasurements.list(owner).filter(row=>row.syncStatus==='Pending').slice(0,100);
    if(!rows.length)return {synced:0,duplicates:0};
    const batch: Record<string,unknown>={deviceSyncTime:new Date().toISOString(),vitalSignReadings:rows.filter(r=>r.endpoint==='/vitalsigns').map(r=>r.payload),sleepRecords:rows.filter(r=>r.endpoint==='/sleep').map(r=>r.payload),moodEntries:rows.filter(r=>r.endpoint==='/mood').map(r=>r.payload)};
    const response=await this.syncOfflineBatch(batch);
    if(owner!==this.measurementOwner())throw new Error('Session changed. Reopen the original astronaut account to synchronize.');
    const ids=new Set(rows.map(r=>r.clientGeneratedId));
    const results=(response.itemResults || []).filter((r:any)=>ids.has(r.clientGeneratedId));
    this.offlineMeasurements.settle(owner,results);
    window.dispatchEvent(new Event('app:queue-changed'));
    return {synced:results.filter((r:any)=>r.status==='Accepted').length,duplicates:results.filter((r:any)=>r.status==='Duplicate').length};
  }
  public getGuides(): Promise<any[]> {
    return this.cachedReference('/guides');
  }
  public getReferenceRanges(): Promise<any[]> {
    return this.request<any[]>('/reference-ranges');
  }
  public getEmergencyProcedures(): Promise<any[]> {
    return this.cachedReference('/emergency-procedures');
  }
  public cachedGuideDate: string | null = null;
  private async cachedReference(endpoint: string): Promise<any[]> {
    try {
      const items=await this.request<any[]>(endpoint);
      try {localStorage.setItem('nasa_reference_cache:'+endpoint,JSON.stringify({items,savedAt:new Date().toISOString()}));}
      catch { /* Caching is optional; the successful server response remains usable. */ }
      this.cachedGuideDate=null;return items;
    } catch(error) {
      if((error as Error).name!=='TypeError')throw error;
      const cached=localStorage.getItem('nasa_reference_cache:'+endpoint);
      if(!cached)throw error;
      const value=JSON.parse(cached);
      if(!Array.isArray(value.items))throw error;
      this.cachedGuideDate=value.savedAt;return value.items;
    }
  }
  public getAssignmentReminders(astronautId?: number): Promise<AssignmentReminders> {
    return this.request('/reminders' + (astronautId ? '?astronautId=' + astronautId : ''));
  }
  public updateAssignmentReminder(id: number, settings: { enabled: boolean; intervalMinutes: number; soundEnabled: boolean }): Promise<void> {
    return this.request(`/reminders/${id}/settings`, {method:'PUT',body:JSON.stringify(settings)});
  }
  public sendAssignmentReminder(id: number): Promise<ReminderDelivery> {
    return this.submissions.run('reminder-send:'+id,()=>this.request(`/reminders/${id}/send`,{method:'POST'}));
  }
  public acknowledgeAssignmentReminder(id: number, token: string): Promise<void> {
    return this.request(`/reminders/${id}/acknowledge`,{method:'POST',body:JSON.stringify({token})});
  }
  private submissions = new Submissions();
  private reads = new Map<string, Promise<unknown>>();
  private writeSequence = 0;
  public getHabits(astronautId?: number): Promise<HabitDashboard> {
    return this.request('/habits' + (astronautId ? '?astronautId=' + astronautId : ''));
  }
  public completeHabit(id: number): Promise<HabitDashboard> {
    return this.submissions.run('/habits/' + id + '/complete', () => this.request<HabitDashboard>('/habits/' + id + '/complete', { method: 'POST' }).then(result => { window.dispatchEvent(new CustomEvent('app:assignment-completed',{detail:id})); window.dispatchEvent(new Event('app:reminders-refresh')); return result; }));
  }
  private refreshInFlight: Promise<boolean> | null = null;
  private accessToken: string | null = localStorage.getItem('nasa_access_token');
  private refreshToken: string | null = localStorage.getItem('nasa_refresh_token');

  public setTokens(access: string, refresh: string) {
    this.accessToken = access;
    this.refreshToken = refresh;
    localStorage.setItem('nasa_access_token', access);
    localStorage.setItem('nasa_refresh_token', refresh);
  }

  public clearTokens() {
    this.submissions.clear();
    this.accessToken = null;
    this.refreshToken = null;
    localStorage.removeItem('nasa_access_token');
    localStorage.removeItem('nasa_refresh_token');
  }

  public getAccessToken(): string | null {
    return this.accessToken;
  }

  public async getValidAccessToken(): Promise<string> {
    if (this.accessToken && this.refreshToken) {
      try {
        const encoded = this.accessToken.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
        const claims = JSON.parse(atob(encoded));
        if (typeof claims.exp === 'number' && claims.exp * 1000 < Date.now() + 30000) await this.refreshTokens();
      } catch { /* An opaque or malformed token will be rejected by the server. */ }
    }
    return this.accessToken || '';
  }

  private request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const isRead = !options.method || options.method === 'GET';
    const key = (this.accessToken || '') + ':' + endpoint;
    if (isRead && this.reads.has(key)) return this.reads.get(key) as Promise<T>;
    const mutation = !isRead && !endpoint.startsWith('/auth/') && !endpoint.endsWith('/read');
    const id = ++this.writeSequence;
    const notify = (phase: string) => { if (mutation) window.dispatchEvent(new CustomEvent('app:write', {detail:{id,phase}})); };
    notify('saving');
    const request = this.performRequest<T>(endpoint, options).then(result => { notify('saved'); return result; }).catch(error => { notify('error'); reportError(error); throw error; }).finally(() => { if (isRead) this.reads.delete(key); });
    if (isRead) this.reads.set(key, request);
    return request;
  }

  private async performRequest<T>(endpoint: string, options: RequestInit): Promise<T> {
    if (typeof options.body === 'string') {
      options = { ...options, body: JSON.stringify(convertContract(JSON.parse(options.body), endpoint, true)) };
    }
    const tokenAtRequest = this.accessToken;
    const url = `${API_BASE}${endpoint}`;
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers as Record<string, string>),
    };

    if (this.accessToken) {
      headers['Authorization'] = `Bearer ${this.accessToken}`;
    }

    let response = await fetch(url, { ...options, headers });

    if (response.status === 401 && this.refreshToken && !['/auth/login', '/auth/refresh', '/auth/logout'].includes(endpoint)) {
      const refreshed = this.accessToken !== tokenAtRequest || await this.refreshTokens();
      if (refreshed && this.accessToken) {
        headers['Authorization'] = `Bearer ${this.accessToken}`;
        response = await fetch(url, { ...options, headers });
      }
    }

    if (!response.ok) {
      if (response.status === 401 && !endpoint.startsWith('/auth/login')) {
        window.dispatchEvent(new Event('app:unauthorized'));
      }
      const errorData = await response.json().catch(() => ({}));
      const details = errorData.errors || errorData;
      const validation = Object.values(details).flat().filter((v) => typeof v === 'string').join(' ');
      // Only display deliberate public server messages, never arbitrary database exception text.
      const publicCodes = ['DATABASE_UPDATE_REQUIRED', 'DATABASE_AUTH_FAILED', 'DATABASE_BUSY', 'DATABASE_UNAVAILABLE', 'REQUEST_FAILED'];
      const isPublicFailure = publicCodes.includes(errorData.code);
      const arabic = localStorage.getItem('nasa_lang') === 'ar';
      const message = response.status >= 500
        ? isPublicFailure ? (arabic && errorData.messageAr || errorData.message) : (arabic ? 'تعذّر على الخادم إتمام الطلب. حاول مرة أخرى.' : 'The server could not complete the request. Please retry.')
        : errorData.message || errorData.detail || validation || errorData.title || `HTTP ${response.status}`;
      const traceId = typeof errorData.traceId === 'string' ? errorData.traceId : response.headers.get('X-Request-ID');
      const error = Object.assign(new Error(message + (traceId ? ` (${arabic ? 'رقم الطلب' : 'Request ID'}: ${traceId})` : '')), {code:errorData.code, status:response.status, traceId});
      throw error;
    }

    if (response.status === 204) {
      return {} as T;
    }

    if (!response.headers.get('content-type')?.includes('application/json')) {
      throw new Error('The API returned an unexpected response. Check the API URL and reverse proxy.');
    }
    return convertContract(await response.json(), endpoint) as T;
  }

  private async refreshTokens(): Promise<boolean> {
    if (!this.refreshInFlight) {
      this.refreshInFlight = this.performRefresh().finally(() => { this.refreshInFlight = null; });
    }
    return this.refreshInFlight;
  }

  private async performRefresh(): Promise<boolean> {
    const requestedRefreshToken = this.refreshToken;
    try {
      const res = await fetch(`${API_BASE}/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: this.refreshToken }),
      });

      if (!res.ok) {
        if (this.refreshToken === requestedRefreshToken && (res.status === 400 || res.status === 401)) this.clearTokens();
        return false;
      }

      const data = await res.json();
      if (this.refreshToken !== requestedRefreshToken) return false;
      const nextAccessToken = data.token || data.accessToken;
      if (nextAccessToken && data.refreshToken) {
        this.setTokens(nextAccessToken, data.refreshToken);
        return true;
      }
      return false;
    } catch (error) {
      reportError(error);
      return false;
    }
  }

  // 1. AuthController (/api/auth/*)
  public async login(username: string, password: string): Promise<any> {
    const data = await this.request<any>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    });
    const nextAccessToken = data.token || data.accessToken;
    if (nextAccessToken && data.refreshToken) {
      this.setTokens(nextAccessToken, data.refreshToken);
    }
    return data;
  }

  public async logout(): Promise<any> {
    try { return await this.request<any>('/auth/logout', {
      method: 'POST',
      body: JSON.stringify({ refreshToken: this.refreshToken }),
    }); } finally { this.clearTokens(); }
  }

  public async getCurrentUser(): Promise<any> {
    return this.request<any>('/auth/me');
  }

  // 2. DashboardController (/api/dashboard/*)
  public async getDashboardMe(): Promise<any> {
    return this.request<any>('/dashboard/me');
  }

  public async getMonitorDashboard(): Promise<any> {
    return this.request<any>('/dashboard/monitor');
  }

  public async getAstronautSnapshot(id: number): Promise<any> {
    return this.request<any>(`/dashboard/astronauts/${id}`);
  }

  // 3. VitalSignsController (/api/vitalsigns/*)
  public async getVitalById(id: number): Promise<VitalSignReading> {
    return this.request<VitalSignReading>(`/vitalsigns/${id}`);
  }

  public async getLatestVitals(astronautId?: number): Promise<VitalSignReading[]> {
    const query = astronautId ? `?astronautId=${astronautId}` : '';
    return this.request<VitalSignReading[]>(`/vitalsigns/latest${query}`);
  }

  public async getVitalsHistory(
    pageNumber = 1,
    pageSize = 25,
    astronautId?: number,
    fromUtc?: string,
    toUtc?: string
  ): Promise<any> {
    const params = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });
    if (astronautId) params.set('astronautId', String(astronautId));
    if (fromUtc) params.set('fromUtc', fromUtc);
    if (toUtc) params.set('toUtc', toUtc);
    return this.request<any>(`/vitalsigns/history?${params.toString()}`);
  }

  public async getVitalTypes(): Promise<any[]> {
    return this.request<any[]>('/vitalsigns/types');
  }

  public async getVitalsForPeriod(fromUtc: string): Promise<VitalSignReading[]> {
    const readings: VitalSignReading[] = [];
    for (let page = 1; ; page++) {
      const result = await this.getVitalsHistory(page, 100, undefined, fromUtc);
      const items = Array.isArray(result.items) ? result.items : [];
      readings.push(...items);
      if (items.length < 100 || readings.length >= result.totalCount) return readings;
    }
  }

  public async recordVital(payload: CreateVitalReadingPayload): Promise<VitalSignReading> {
    const typeMap: Record<string, number> = {
      HR: 1,
      BP: 2,
      SPO2: 3,
      TEMP: 4,
      GLUCOSE: 5,
      RESP: 6,
    };
    return this.submissions.run('/vitalsigns:'+JSON.stringify(payload), identity => this.measurement<VitalSignReading>('/vitalsigns', {
      method: 'POST',
      body: JSON.stringify({
        ...identity,
        vitalSignTypeId: typeMap[payload.vitalSignTypeCode] || 1,
        value: payload.value,
        secondaryValue: payload.secondaryValue,
        bodyPosition: payload.bodyPosition || 'MicrogravityFloating',
        activityContext: payload.activityContext || 'Resting',
        mealContext: payload.mealContext,
        sensorLocation: payload.sensorLocation || 'Thoracic Biometric Vest',
        measurementMethod: 'Automated Telemetry Sensor',
        notes: payload.notes,
      }),
    }));
  }

  // 4. HealthAlertsController (/api/alerts/*)
  public async getAlerts(status?: string, severity?: string, astronautId?: number): Promise<any> {
    const params = new URLSearchParams();
    if (status) params.set('status', status);
    if (severity) params.set('severity', severity);
    if (astronautId) params.set('astronautId', String(astronautId));
    const qs = params.toString();
    return this.request<any>(`/alerts${qs ? `?${qs}` : ''}`);
  }

  public async getAlertById(id: number): Promise<HealthAlert> {
    return this.request<HealthAlert>(`/alerts/${id}`);
  }

  public async triggerManualAlert(
    title: string,
    message: string,
    severity: 'MedicalAttention' | 'Emergency',
    astronautId: number
  ): Promise<HealthAlert> {
    return this.request<HealthAlert>('/alerts/manual', {
      method: 'POST',
      body: JSON.stringify({ astronautId, title, message, severity }),
    });
  }

  public async acknowledgeAlert(
    id: number,
    actionTaken = 'Reviewed by Flight Surgeon'
  ): Promise<HealthAlert> {
    return this.request<HealthAlert>(`/alerts/${id}/acknowledge`, {
      method: 'POST',
      body: JSON.stringify({ actionTaken }),
    });
  }

  public async resolveAlert(id: number, resolutionNotes: string): Promise<HealthAlert> {
    return this.request<HealthAlert>(`/alerts/${id}/resolve`, {
      method: 'POST',
      body: JSON.stringify({ resolutionNotes }),
    });
  }

  // 5. SleepController (/api/sleep/*)
  public async getSleepById(id: number): Promise<SleepRecord> {
    return this.request<SleepRecord>(`/sleep/${id}`);
  }

  public async getLatestSleep(astronautId?: number): Promise<SleepRecord> {
    const qs = astronautId ? `?astronautId=${astronautId}` : '';
    return this.request<SleepRecord>(`/sleep/latest${qs}`);
  }

  public async getSleepHistory(pageNumber = 1, pageSize = 20, astronautId?: number): Promise<any> {
    const params = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });
    if (astronautId) params.set('astronautId', String(astronautId));
    return this.request<any>(`/sleep/history?${params.toString()}`);
  }

  public async recordSleep(payload: Partial<SleepRecord>): Promise<SleepRecord> {
    return this.submissions.run('/sleep:'+JSON.stringify(payload), identity => this.measurement<SleepRecord>('/sleep', {method:'POST',body:JSON.stringify({...payload,...identity})}));
  }

  // 6. MoodController (/api/mood/*)
  public async getMoodById(id: number): Promise<MoodEntry> {
    return this.request<MoodEntry>(`/mood/${id}`);
  }

  public async getLatestMood(astronautId?: number): Promise<MoodEntry> {
    const qs = astronautId ? `?astronautId=${astronautId}` : '';
    return this.request<MoodEntry>(`/mood/latest${qs}`);
  }

  public async getMoodHistory(pageNumber = 1, pageSize = 20, astronautId?: number): Promise<any> {
    const params = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });
    if (astronautId) params.set('astronautId', String(astronautId));
    return this.request<any>(`/mood/history?${params.toString()}`);
  }

  public async recordMood(payload: Partial<MoodEntry>): Promise<MoodEntry> {
    return this.submissions.run('/mood:'+JSON.stringify(payload), identity => this.measurement<MoodEntry>('/mood', {method:'POST',body:JSON.stringify({...payload,...identity})}));
  }

  // 7. RecommendationsController (/api/recommendations/*)
  public async getRecommendations(astronautId?: number, pendingOnly = false): Promise<any> {
    const params = new URLSearchParams({ pendingOnly: String(pendingOnly) });
    if (astronautId) params.set('astronautId', String(astronautId));
    return this.request<any>(`/recommendations?${params.toString()}`);
  }

  public async getRecommendationById(id: number): Promise<Recommendation> {
    return this.request<Recommendation>(`/recommendations/${id}`);
  }

  public async updateRecommendationStatus(
    id: number,
    status: RecommendationStatus
  ): Promise<Recommendation> {
    return this.request<Recommendation>(`/recommendations/${id}/status`, {
      method: 'PUT',
      body: JSON.stringify({ status }),
    }).then(result => {
      if(status==='Completed'||status==='Dismissed') { window.dispatchEvent(new CustomEvent('app:assignment-completed',{detail:id}));window.dispatchEvent(new Event('app:reminders-refresh')); }
      return result;
    });
  }

  public async createRecommendation(payload: any): Promise<Recommendation> {
    return this.request<Recommendation>('/recommendations', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  }

  // 8. SymptomGuidanceController (/api/symptoms/*)
  public async getSymptoms(): Promise<any[]> {
    return this.request<any[]>('/symptoms');
  }

  public async getSpaceConditions(search?: string): Promise<any[]> {
    const qs = search ? `?search=${encodeURIComponent(search)}` : '';
    return this.request<any[]>(`/symptoms/guidance${qs}`);
  }

  public async assessSymptoms(astronautId: number, symptomCodes: string[], severity = 'mild', durationHours = 0): Promise<any> {
    return this.request<any>('/symptoms/assess', {
      method: 'POST',
      body: JSON.stringify({ astronautId, symptomCodes, severity, durationHours }),
    });
  }

  // 9. ChatController (/api/chat/*)
  public async getChatMessages(astronautId?: number, pageSize = 200): Promise<any> {
    const params = new URLSearchParams({ pageNumber: '1', pageSize: String(pageSize) });
    if (astronautId) params.set('astronautId', String(astronautId));
    return this.request<any>(`/chat/messages?${params.toString()}`);
  }

  public async sendChatMessage(
    content: string,
    astronautId?: number,
    clientGeneratedId?: string
  ): Promise<any> {
    return this.request<any>('/chat/messages', {
      method: 'POST',
      body: JSON.stringify({
        clientGeneratedId: clientGeneratedId || crypto.randomUUID(),
        astronautId,
        content,
        recordedAt: new Date().toISOString(),
      }),
    });
  }

  public async markChatMessageRead(id: number): Promise<ChatMessage> {
    return this.request<ChatMessage>(`/chat/messages/${id}/read`, {
      method: 'POST',
    });
  }

  public async getUnreadChatCount(astronautId?: number): Promise<any> {
    const query = astronautId ? `?astronautId=${astronautId}` : '';
    return this.request<any>(`/chat/unread-count${query}`);
  }

  // 10. AstronautsController (/api/astronauts/*)
  public async getAstronauts(missionId?: number): Promise<any> {
    const qs = missionId ? `?missionId=${missionId}` : '';
    return this.request<any>(`/astronauts${qs}`);
  }

  public async getAstronautMe(): Promise<any> {
    return this.request<any>('/astronauts/me');
  }

  public async getAstronautById(id: number): Promise<any> {
    return this.request<any>(`/astronauts/${id}`);
  }

  public async createAstronaut(payload: any): Promise<any> {
    return this.request<any>('/astronauts', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  }

  public async updateAstronaut(id: number, payload: any): Promise<any> {
    return this.request<any>(`/astronauts/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    });
  }

  public async deactivateAstronaut(id: number): Promise<any> {
    return this.request<any>(`/astronauts/${id}/deactivate`, {
      method: 'POST',
    });
  }

  // 11. MissionsController (/api/missions/*)
  public async getMissions(): Promise<any[]> {
    return this.request<any[]>('/missions');
  }

  public async getMissionById(id: number): Promise<any> {
    return this.request<any>(`/missions/${id}`);
  }

  public async createMission(payload: any): Promise<any> {
    return this.request<any>('/missions', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  }

  public async updateMission(id: number, payload: any): Promise<any> {
    return this.request<any>(`/missions/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    });
  }

  public async deleteMission(id: number): Promise<any> {
    return this.request<any>(`/missions/${id}`, {
      method: 'DELETE',
    });
  }

  public async assignAstronautToMission(missionId: number, astronautId: number): Promise<any> {
    return this.request<any>(`/missions/${missionId}/astronauts/${astronautId}`, {
      method: 'POST',
    });
  }

  public async unassignAstronautFromMission(missionId: number, astronautId: number): Promise<any> {
    return this.request<any>(`/missions/${missionId}/astronauts/${astronautId}`, {
      method: 'DELETE',
    });
  }

  // 12. SyncController (/api/sync)
  public async syncOfflineBatch(batch: any): Promise<any> {
    return this.request<any>('/sync', {
      method: 'POST',
      body: JSON.stringify(batch),
    });
  }
}

export const api = new ApiService();
