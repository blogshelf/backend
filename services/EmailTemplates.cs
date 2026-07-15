using backend.Utils;

namespace backend.services;

public static class EmailTemplates
{
    public static string OtpVerification(string emojiCode) => OtpTemplate.Replace("__CODE__",emojiCode);

    public static string OtpVerification(byte[] code) => OtpTemplate.Replace("__CODE__", EmojiCodec.Encode(code));

    private static string OtpTemplate =>
        """
        <!DOCTYPE html>
        <html lang="en">
        <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <meta name="color-scheme" content="light dark">
        <meta name="supported-color-schemes" content="light dark">
        <title>BlogShelf Authentication</title>
        </head>

        <body style="
            margin:0;
            padding:40px;
            background:#000000;
            color:#FFFFFF;
            font-family:'IBM Plex Mono','Maple Mono',Consolas,monospace;
        ">

        <table align="center"
               cellpadding="0"
               cellspacing="0"
               width="720"
               style="
                    border-collapse:collapse;
                    border:1px solid #FFFFFF;
                    background:#090909;
        ">

        <tr>
        <td style="padding:24px 32px;">

        <div style="
        font-size:22px;
        font-weight:bold;
        letter-spacing:2px;
        color:#FFFFFF;
        ">
        BLOGSHELF.IDENTITY
        </div>

        <div style="
        margin-top:6px;
        font-size:13px;
        color:#BFBFBF;
        ">
        AUTH SERVICE · BUILD 2026.07
        </div>

        </td>
        </tr>

        <tr>
        <td style="border-top:1px solid #FFFFFF;"></td>
        </tr>

        <tr>
        <td style="padding:36px 32px;">

        <div style="
        font-size:13px;
        color:#BFBFBF;
        ">
        REQUEST
        </div>

        <div style="
        margin-top:6px;
        font-size:28px;
        font-weight:bold;
        letter-spacing:1px;
        color:#FFFFFF;
        ">
        EMAIL VERIFICATION
        </div>

        <p style="
        margin-top:28px;
        line-height:1.8;
        font-size:14px;
        color:#D9D9D9;
        ">
        A verification token has been generated for the current authentication session.
        Enter this token only on an official BlogShelf authentication page.
        </p>

        <div style="margin-top:40px;">

        <div style="
        font-size:13px;
        color:#4DA3FF;
        margin-bottom:10px;
        ">
        TOKEN
        </div>

        <div style="
        border-top:1px solid #FFFFFF;
        border-bottom:1px solid #FFFFFF;
        padding:24px 0;
        text-align:center;
        font-size:34px;
        letter-spacing:10px;
        color:#FFFFFF;
        ">
         __CODE__
        </div>

        </div>

        <table style="
        margin-top:36px;
        width:100%;
        border-collapse:collapse;
        font-size:13px;
        ">

        <tr>
        <td style="padding:6px 0;color:#BFBFBF;width:180px;">STATE</td>
        <td style="padding:6px 0;color:#FFFFFF;">● VALID</td>
        </tr>

        <tr>
        <td style="padding:6px 0;color:#BFBFBF;">CHANNEL</td>
        <td style="padding:6px 0;color:#FFFFFF;">EMAIL</td>
        </tr>

        <tr>
        <td style="padding:6px 0;color:#BFBFBF;">EXPIRES</td>
        <td style="padding:6px 0;color:#FFFFFF;">05:00</td>
        </tr>

        <tr>
        <td style="padding:6px 0;color:#BFBFBF;">REQUEST TYPE</td>
        <td style="padding:6px 0;color:#FFFFFF;">AUTH-VERIFY</td>
        </tr>

        </table>

        <div style="
        margin-top:40px;
        border-top:1px solid #FFFFFF;
        padding-top:24px;
        ">

        <div style="
        font-size:13px;
        color:#4DA3FF;
        margin-bottom:18px;
        ">
        SECURITY NOTICE
        </div>

        <div style="
        font-size:13px;
        line-height:1.9;
        color:#D9D9D9;
        ">

        • BlogShelf will never request this token by email,
        chat, phone, or direct message.<br><br>

        • Only enter this token after verifying the browser
        address belongs to an official BlogShelf domain.<br><br>

        • If you did not initiate this request, simply ignore
        this message. No further action is required.

        </div>

        </div>

        </td>
        </tr>

        <tr>
        <td style="border-top:1px solid #FFFFFF;"></td>
        </tr>

        <tr>
        <td style="
        padding:24px 32px;
        font-size:12px;
        line-height:1.8;
        color:#BFBFBF;
        ">

        Generated automatically by BlogShelf Authentication Service.<br>
        Mail Gateway Subsystem · AUTH SERVICE<br>
        No reply required.

        </td>
        </tr>

        </table>

        </body>
        </html>
        """;
}
