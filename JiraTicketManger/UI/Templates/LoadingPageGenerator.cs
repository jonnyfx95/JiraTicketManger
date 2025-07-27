using System;

namespace JiraTicketManager.UI.Templates
{
    public static class LoadingPageGenerator
    {
        public static string GenerateLoadingPage(string title = "Autenticazione in corso", string subtitle = "Verifica credenziali Microsoft SSO")
        {
            return $@"
<!DOCTYPE html>
<html lang='it'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Dedagroup - Autenticazione</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            overflow: hidden;
        }}
        
        .container {{
            text-align: center;
            background: rgba(255, 255, 255, 0.95);
            padding: 60px 40px;
            border-radius: 20px;
            box-shadow: 0 20px 40px rgba(0, 0, 0, 0.1);
            backdrop-filter: blur(10px);
            max-width: 400px;
            width: 90%;
        }}
        
        .logo {{
            width: 80px;
            height: 80px;
            background: linear-gradient(45deg, #0078d4, #106ebe);
            border-radius: 50%;
            margin: 0 auto 30px;
            display: flex;
            align-items: center;
            justify-content: center;
            box-shadow: 0 10px 20px rgba(0, 120, 212, 0.3);
        }}
        
        .logo::before {{
            content: '🏢';
            font-size: 35px;
            color: white;
        }}
        
        .spinner {{
            width: 50px;
            height: 50px;
            border: 4px solid #f3f3f3;
            border-top: 4px solid #0078d4;
            border-radius: 50%;
            animation: spin 1s linear infinite;
            margin: 20px auto;
        }}
        
        @keyframes spin {{
            0% {{ transform: rotate(0deg); }}
            100% {{ transform: rotate(360deg); }}
        }}
        
        .title {{
            font-size: 24px;
            font-weight: 600;
            color: #2c3e50;
            margin-bottom: 10px;
            line-height: 1.2;
        }}
        
        .subtitle {{
            font-size: 16px;
            color: #7f8c8d;
            margin-bottom: 30px;
            line-height: 1.4;
        }}
        
        .progress-bar {{
            width: 100%;
            height: 4px;
            background: #ecf0f1;
            border-radius: 2px;
            overflow: hidden;
            margin-top: 20px;
        }}
        
        .progress-fill {{
            height: 100%;
            background: linear-gradient(90deg, #0078d4, #106ebe);
            border-radius: 2px;
            animation: progress 2s ease-in-out infinite;
        }}
        
        @keyframes progress {{
            0% {{ width: 0%; }}
            50% {{ width: 70%; }}
            100% {{ width: 100%; }}
        }}
        
        .footer {{
            position: absolute;
            bottom: 30px;
            left: 50%;
            transform: translateX(-50%);
            font-size: 12px;
            color: rgba(255, 255, 255, 0.8);
            text-align: center;
        }}
        
        .dots {{
            display: inline-block;
            animation: dots 2s ease-in-out infinite;
        }}
        
        @keyframes dots {{
            0%, 20% {{ opacity: 0; }}
            50% {{ opacity: 1; }}
            100% {{ opacity: 0; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='logo'></div>
        <h1 class='title'>{title}<span class='dots'>...</span></h1>
        <p class='subtitle'>{subtitle}</p>
        <div class='spinner'></div>
        <div class='progress-bar'>
            <div class='progress-fill'></div>
        </div>
    </div>
    <div class='footer'>
        <strong>Dedagroup</strong><br>
        Jira Ticket Manager v2.0
    </div>
</body>
</html>";
        }

        public static string GenerateSuccessPage(string userEmail)
        {
            return GenerateLoadingPage(
                "Accesso Autorizzato ✓",
                $"Benvenuto {userEmail}"
            );
        }

        public static string GenerateErrorPage(string errorMessage)
        {
            return GenerateLoadingPage(
                "Accesso Negato ✗",
                errorMessage
            );
        }
    }
}