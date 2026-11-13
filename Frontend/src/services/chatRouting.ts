export interface ChatRecipient {
  id: string;
  role: 'Astronaut' | 'Monitor';
  astronautId?: number;
  titleAr: string;
  titleEn: string;
  roleLabelAr: string;
  roleLabelEn: string;
  iconType: 'doctor' | 'astronaut';
}

export function chatRecipients(role: 'Astronaut' | 'Monitor', crew: Array<{
  id: number; fullName: string; fullNameAr?: string; astronautCode: string; isActive?: boolean;
}>): ChatRecipient[] {
  if (role === 'Astronaut') return [{
    id: 'medical-team', role: 'Monitor', titleEn: 'Medical team', titleAr: 'الفريق الطبي',
    roleLabelEn: 'Doctor / Monitor', roleLabelAr: 'الطبيب / المراقب', iconType: 'doctor',
  }];
  if (role !== 'Monitor') return [];
  return crew.filter(member => member.id > 0 && member.isActive !== false).map(member => ({
    id: `astronaut-${member.id}`, role: 'Astronaut', astronautId: member.id,
    titleEn: member.fullName, titleAr: member.fullNameAr || member.fullName,
    roleLabelEn: `Astronaut (${member.astronautCode})`, roleLabelAr: `رائد فضاء (${member.astronautCode})`,
    iconType: 'astronaut',
  }));
}

