using AstronautHealthMonitoring.DTOs.SymptomGuidance;

namespace AstronautHealthMonitoring.Services.Implementations;

public static class AssociationGuidance
{
    public static void Apply(SymptomAssessmentResponseDto result,string locale)
    {
        var ar=locale=="ar";
        string Text(string en,string arabic)=>ar?arabic:en;
        var actions=new List<string>();
        if(result.Level==3)actions.Add(Text("Alert crew and the medical officer now and follow the mission emergency protocol.","نبّه الطاقم والمسؤول الطبي الآن واتبع بروتوكول طوارئ المهمة."));
        if(result.Radiation.Status=="rule_triggered")actions.Add(Text("Follow the mission shelter / radiation protocol now.","اتبع بروتوكول الاحتماء أو الإشعاع الخاص بالمهمة الآن."));
        var top=result.ConditionMatches.FirstOrDefault()?.ConditionCode;
        actions.Add(top switch {
            "SPACE_MOTION_SICKNESS"=>Text("Move slowly and avoid abrupt head movements.","تحرّك ببطء وتجنب حركات الرأس المفاجئة."),
            "SPACEFLIGHT_VISION_CHANGES"=>Text("Report any vision change, especially near vision. Schedule the eye exam from the mission plan.","أبلغ عن أي تغيّر في الرؤية، خصوصًا الرؤية القريبة. رتّب فحص العين ضمن خطة المهمة."),
            "CLOSED_ENVIRONMENT_AIR_QUALITY"=>Text("Check cabin CO2 and ventilation readings and follow the air-quality procedure.","راجع قراءات ثاني أكسيد الكربون والتهوية في المقصورة واتبع إجراء جودة الهواء."),
            "SLEEP_DEFICIENCY"=>Text("Protect your sleep window and reduce light and noise. Log how long you slept.","احمِ وقت نومك وقلّل الضوء والضوضاء. سجّل مدة نومك."),
            "STRESS_ISOLATION"=>Text("Talk to a crew member or ground support. Take structured downtime.","تحدّث مع أحد أفراد الطاقم أو الدعم الأرضي. خصّص وقتًا منظّمًا للراحة."),
            "DEHYDRATION_NUTRITION"=>Text("Drink water on schedule and log your intake.","اشرب الماء وفق الجدول وسجّل الكمية."),
            "BLOOD_SUGAR_CHANGE"=>Text("Eat on schedule and log your meals.","تناول الطعام وفق الجدول وسجّل وجباتك."),
            "TEMPERATURE_CHANGE"=>Text("Adjust clothing or cabin temperature per procedure and log it.","عدّل الملابس أو حرارة المقصورة وفق الإجراء وسجّل ذلك."),
            "IMMUNE_SKIN_CHANGES"=>Text("Follow the skin-care procedure and report a rash that persists or blisters.","اتبع إجراء العناية بالجلد وأبلغ عن الطفح المستمر أو المصحوب ببثور."),
            "ORTHOSTATIC_INTOLERANCE"=>Text("Sit or lie down before standing and rise slowly.","اجلس أو استلقِ قبل الوقوف، وانهض ببطء."),
            _=>Text("Log your symptoms and contact the medical officer if you are concerned.","سجّل الأعراض وتواصل مع المسؤول الطبي عند القلق.")});
        if(result.GuidanceMode=="DELAYED")actions.Add(Text("Communication is delayed. Contact the medical officer through the mission communication procedure; delivery has not been confirmed.","الاتصال متأخر. تواصل مع المسؤول الطبي وفق إجراء اتصالات المهمة؛ لم يُؤكَّد تسليم رسالة."));
        else if(result.Level>=2)actions.Add(Text("Contact the medical officer.","تواصل مع المسؤول الطبي."));
        result.RecommendedActions=actions.Take(4).ToList();
    }
}
