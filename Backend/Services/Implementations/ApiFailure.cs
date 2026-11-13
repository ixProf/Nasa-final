using System.Net.Sockets;
using Npgsql;

namespace AstronautHealthMonitoring.Services.Implementations;

public record ApiFailure(string Code, int Status, string Message, string MessageAr)
{
    public static ApiFailure Classify(Exception error)
    {
        for (Exception? current=error; current!=null; current=current.InnerException)
        {
            if(current is PostgresException pg)
            {
                if(pg.SqlState is "42P01" or "42703") return Schema;
                if(pg.SqlState is "28P01" or "28000") return new("DATABASE_AUTH_FAILED",503,"Database sign-in failed. Check the backend database credentials.","تعذّر تسجيل الدخول إلى قاعدة البيانات. تحقق من بيانات الاتصال في إعدادات الخادم.");
                if(pg.SqlState=="53300") return new("DATABASE_BUSY",503,"The database connection limit was reached. Wait briefly and retry.","بلغت اتصالات قاعدة البيانات الحد الأقصى. انتظر قليلًا وأعد المحاولة.");
            }
            if(current is SocketException or TimeoutException || current is NpgsqlException and not PostgresException) return Unavailable;
        }
        return new("REQUEST_FAILED",500,"The request failed. Use the request ID to locate the exception in the backend logs.","تعذّر إتمام الطلب. استخدم معرّف الطلب للوصول إلى تفاصيل الخطأ في سجل الخادم.");
    }
    public static ApiFailure Schema => new("DATABASE_UPDATE_REQUIRED",503,"A database update is required. Stop the backend, run dotnet ef database update in Backend, then restart it.","تحتاج قاعدة البيانات إلى تحديث. أوقف الخادم، وشغّل dotnet ef database update من مجلد Backend، ثم أعد تشغيله.");
    public static ApiFailure Unavailable => new("DATABASE_UNAVAILABLE",503,"The backend cannot reach the database. Check the Supabase hostname, network access and connection settings.","الخادم لا يستطيع الوصول إلى قاعدة البيانات. تحقق من عنوان سوبابيس واتصال الشبكة وإعدادات الاتصال.");
}
