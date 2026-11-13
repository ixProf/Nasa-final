import { convertContract } from './contract';
import { HUB_BASE } from './config';
import { reportError } from './errors';
import * as signalR from '@microsoft/signalr';
import { HealthAlert } from '../types/telemetry';

type AlertCallback = (alert: HealthAlert) => void;
type ChatCallback = (msg: any) => void;

/**
 * Real-time hubs bound to ONE authenticated session.
 * `connect()` always tears down any previous connection first, so the server
 * re-evaluates the new token and places the socket in the correct groups
 * (`monitors` or `astronaut-{id}`). `disconnect()` must be called on logout.
 */
class SignalRService {
  private generation = 0;
  private alertsHub: signalR.HubConnection | null = null;
  private chatHub: signalR.HubConnection | null = null;
  private alertListeners: AlertCallback[] = [];
  private chatListeners: ChatCallback[] = [];
  private readListeners: ChatCallback[] = [];

  private build(url: string, getToken: () => string | Promise<string>) {
    return new signalR.HubConnectionBuilder()
      .withUrl(url, {
        accessTokenFactory: getToken,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }

  public async connect(tokenOrGetter?: string | (() => string | Promise<string>)) {
    const generation = ++this.generation;
    await this.stopHubs();
    if (generation !== this.generation) return;

    const getToken = () => {
      if (typeof tokenOrGetter === 'function') {
        return tokenOrGetter() || '';
      }
      if (typeof tokenOrGetter === 'string' && tokenOrGetter) {
        return tokenOrGetter;
      }
      return localStorage.getItem('nasa_access_token') || '';
    };

    this.alertsHub = this.build(`${HUB_BASE}/alerts`, getToken);
    const notifyAlert = (alert: HealthAlert) => this.alertListeners.forEach((cb) => cb(convertContract(alert,'/alerts')));
    this.alertsHub.on('AlertCreated', notifyAlert);
    this.alertsHub.on('AlertAcknowledged', notifyAlert);
    this.alertsHub.on('AlertResolved', notifyAlert);
    this.alertsHub.on('AssignmentReminder', reminder => window.dispatchEvent(new CustomEvent('app:assignment-reminder', { detail: reminder })));

    this.chatHub = this.build(`${HUB_BASE}/chat`, getToken);
    this.chatHub.on('ReceiveMessage', (msg) => this.chatListeners.forEach((cb) => cb(msg)));
    this.chatHub.on('MessageRead', (msg) => this.readListeners.forEach((cb) => cb(msg)));

    const start = async (hub: signalR.HubConnection) => {
      hub.onreconnecting(() => reportError(new Error('Live updates interrupted. Reconnecting…')));
      for (let attempt = 0; attempt < 3; attempt++) {
        if (generation !== this.generation) return;
        try { await hub.start(); return; }
        catch (error) {
          if (generation !== this.generation) return;
          if (attempt === 2) { reportError(error); return; }
          await new Promise(resolve => setTimeout(resolve, 2000 * (attempt + 1)));
        }
      }
    };
    await Promise.all([start(this.alertsHub), start(this.chatHub)]);
  }

  public async initConnection(tokenOrGetter?: string | (() => string | Promise<string>)) {
    return this.connect(tokenOrGetter);
  }

  public async disconnect() {
    this.generation++;
    await this.stopHubs();
  }

  private async stopHubs() {
    const hubs = [this.alertsHub, this.chatHub];
    this.alertsHub = null;
    this.chatHub = null;
    await Promise.all(hubs.map((h) => h?.stop().catch(reportError)));
  }

  public onAlertReceived(callback: AlertCallback) {
    this.alertListeners.push(callback);
    return () => {
      this.alertListeners = this.alertListeners.filter((cb) => cb !== callback);
    };
  }

  public onChatReceived(callback: ChatCallback) {
    this.chatListeners.push(callback);
    return () => {
      this.chatListeners = this.chatListeners.filter((cb) => cb !== callback);
    };
  }

  public onChatRead(callback: ChatCallback) {
    this.readListeners.push(callback);
    return () => {
      this.readListeners = this.readListeners.filter((cb) => cb !== callback);
    };
  }

  public isConnected(): boolean {
    return this.chatHub?.state === signalR.HubConnectionState.Connected && this.alertsHub?.state === signalR.HubConnectionState.Connected;
  }
}

export const signalRService = new SignalRService();
