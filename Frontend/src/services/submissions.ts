// One logical record keeps its identity across an uncertain network failure.
export class Submissions {
  private generation = 0;
  private pending = new Map<string, Promise<unknown>>();
  private identities = new Map<string, { clientGeneratedId: string; recordedAt: string }>();
  public run<T>(key: string, write: (identity: { clientGeneratedId: string; recordedAt: string }) => Promise<T>): Promise<T> {
    const generation = this.generation;
    const existing = this.pending.get(key);
    if (existing) return existing as Promise<T>;
    let identity = this.identities.get(key);
    if (!identity) { identity = { clientGeneratedId: crypto.randomUUID(), recordedAt: new Date().toISOString() }; this.identities.set(key, identity); }
    const operation = Promise.resolve().then(() => write(identity!)).then(result => { if (generation === this.generation) this.identities.delete(key); return result; }).finally(() => { if (generation === this.generation) this.pending.delete(key); });
    this.pending.set(key, operation);
    return operation;
  }
  public clear() { this.generation++; this.identities.clear(); this.pending.clear(); }
}
