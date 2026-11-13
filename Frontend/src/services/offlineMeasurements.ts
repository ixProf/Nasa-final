export type MeasurementEndpoint = '/vitalsigns' | '/sleep' | '/mood';
export interface QueuedMeasurement {
  clientGeneratedId: string;
  recordedAt: string;
  endpoint: MeasurementEndpoint;
  payload: Record<string, unknown>;
  syncStatus: 'Pending' | 'Rejected';
  reason?: string;
}

// Records are scoped to the signed-in astronaut. An uncertain upload retains its original GUID.
export class OfflineMeasurements {
  constructor(private storage: Pick<Storage,'getItem'|'setItem'>) {}
  list(owner: string): QueuedMeasurement[] {
    const raw=this.storage.getItem('nasa_offline_measurements:'+owner);
    if(!raw)return [];
    const value=JSON.parse(raw);
    if(!Array.isArray(value))throw new Error('The local measurement queue could not be read.');
    return value;
  }
  save(owner: string, endpoint: MeasurementEndpoint, payload: Record<string,unknown>) {
    const rows=this.list(owner);
    const row: QueuedMeasurement={endpoint,payload,clientGeneratedId:String(payload.clientGeneratedId),recordedAt:String(payload.recordedAt),syncStatus:'Pending'};
    const index=rows.findIndex(r=>r.clientGeneratedId===row.clientGeneratedId);
    if(index>=0)rows[index]=row;else rows.push(row);
    if(rows.length>500)throw new Error('The local queue is full. Synchronize existing measurements first.');
    this.storage.setItem('nasa_offline_measurements:'+owner,JSON.stringify(rows));
  }
  remove(owner:string,id:string) {
    this.storage.setItem('nasa_offline_measurements:'+owner,JSON.stringify(this.list(owner).filter(row=>row.clientGeneratedId!==id)));
  }
  settle(owner:string,results:Array<{clientGeneratedId:string;status:string;reason?:string}>) {
    const byId=new Map(results.map(row=>[row.clientGeneratedId,row]));
    const rows=this.list(owner).flatMap(row=>{
      const result=byId.get(row.clientGeneratedId);
      if(!result)return [row];
      if(result.status==='Accepted'||result.status==='Duplicate')return [];
      return [{...row,syncStatus:'Rejected' as const,reason:result.reason}];
    });
    this.storage.setItem('nasa_offline_measurements:'+owner,JSON.stringify(rows));
  }
}
