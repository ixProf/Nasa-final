import type { AssociationAssessment } from '../components/dashboard/AssociationResults';

export async function collectReportPages<T>(load:(page:number)=>Promise<{items:T[];totalCount?:number;hasNextPage?:boolean}>):Promise<T[]> {
  const rows:T[]=[];
  for(let page=1;;page++) {
    const result=await load(page);
    if(!Array.isArray(result.items))throw new Error('Invalid report response');
    rows.push(...result.items);
    const more=result.hasNextPage ?? (result.totalCount!=null?rows.length<result.totalCount:result.items.length===100);
    if(!more)return rows;
    if(!result.items.length)throw new Error('Incomplete report history');
  }
}
export interface ReportAssessment {
  id:string; recordedAt:string;
  symptoms:Array<{code:string;name:string;nameAr:string;severity:string;durationHours:number}>;
  result:AssociationAssessment;
}
const key=(account:string,astronautId:number)=>'health_report_assessments:'+encodeURIComponent(account)+':'+astronautId;
export function readReportAssessments(account:string,astronautId:number):ReportAssessment[] {
  try {const rows=JSON.parse(sessionStorage.getItem(key(account,astronautId)) || '[]');return Array.isArray(rows)?rows:[];}catch{return [];}
}
export function saveReportAssessment(account:string,astronautId:number,entry:ReportAssessment):boolean {
  try {sessionStorage.setItem(key(account,astronautId),JSON.stringify([entry,...readReportAssessments(account,astronautId)]));return true;}catch{return false;}
}
