using System.Text.RegularExpressions;
using Aptabase.Features.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Features.Stats;

public class DownloadRequest
{
    public string BuildMode { get; set; } = "";
    public string AppId { get; set; } = "";
    public string AppName { get; set; } = "";
    public int Month { get; set; }
    public int Year { get; set; }
}

public class MonthlyUsage
{
    public int Year { get; set; }
    public int Month { get; set; }
    public long Events { get; set; }
}

[ApiController, IsAuthenticated, HasReadAccessToApp]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public partial class ExportController : Controller
{
    private readonly IQueryClient _queryClient;

    public ExportController(IQueryClient queryClient)
    {
        _queryClient = queryClient ?? throw new ArgumentNullException(nameof(queryClient));
    }

    [HttpGet("/api/_export/usage")]
    public async Task<IActionResult> MonthlyUsage([FromQuery] string buildMode, [FromQuery] string appId, CancellationToken cancellationToken)
    {
        var rows = await _queryClient.NamedQueryAsync<MonthlyUsage>("monthly_usage__v1", new {
            app_id = GetAppId(buildMode, appId)
        }, cancellationToken);

        if (buildMode.ToLower() == "debug")
            return Ok(rows.Take(5));

        return Ok(rows);
    }

    [HttpGet("/api/_export/download")]
    public async Task<IActionResult> Download([FromQuery] DownloadRequest body, CancellationToken cancellationToken)
    {
        var query = $@"SELECT timestamp, user_id, session_id, event_name,
                              string_props, numeric_props, os_name, os_version,
                              locale, app_version, app_build_number,
                              engine_name, engine_version,
                              country_code, {COUNTRY_NAME_COLUMN}, region_name
                       FROM events
                       WHERE app_id = '{GetAppId(body.BuildMode, body.AppId)}'
                       AND toStartOfMonth(timestamp) = '{body.Year}-{body.Month}-01'
                       FORMAT CSVWithNames";

        var stream = await _queryClient.StreamResponseAsync(query, cancellationToken);
        var appName = UnsafeCharacters().Replace(body.AppName, "").ToLower();
        var fileName = $"{appName}-{body.BuildMode.ToLower()}-{body.Year}-{body.Month.ToString().PadLeft(2, '0')}.csv";
        return File(stream, "text/csv", fileName);
    }

    private string GetAppId(string buildMode, string appId)
    {
        return buildMode.ToLower() switch
        {
            "debug" => $"{appId}_DEBUG",
            _ => appId,
        };
    }

    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex UnsafeCharacters();

    private static readonly string COUNTRY_NAME_COLUMN = @"
            CASE country_code
                    WHEN 'AF' THEN 'Afghanistan'
                    WHEN 'AX' THEN 'Aland Islands'
                    WHEN 'AL' THEN 'Albania'
                    WHEN 'DZ' THEN 'Algeria'
                    WHEN 'AS' THEN 'American Samoa'
                    WHEN 'AD' THEN 'Andorra'
                    WHEN 'AO' THEN 'Angola'
                    WHEN 'AI' THEN 'Anguilla'
                    WHEN 'AQ' THEN 'Antarctica'
                    WHEN 'AG' THEN 'Antigua And Barbuda'
                    WHEN 'AR' THEN 'Argentina'
                    WHEN 'AM' THEN 'Armenia'
                    WHEN 'AW' THEN 'Aruba'
                    WHEN 'AU' THEN 'Australia'
                    WHEN 'AT' THEN 'Austria'
                    WHEN 'AZ' THEN 'Azerbaijan'
                    WHEN 'BS' THEN 'Bahamas'
                    WHEN 'BH' THEN 'Bahrain'
                    WHEN 'BD' THEN 'Bangladesh'
                    WHEN 'BB' THEN 'Barbados'
                    WHEN 'BY' THEN 'Belarus'
                    WHEN 'BE' THEN 'Belgium'
                    WHEN 'BZ' THEN 'Belize'
                    WHEN 'BJ' THEN 'Benin'
                    WHEN 'BM' THEN 'Bermuda'
                    WHEN 'BT' THEN 'Bhutan'
                    WHEN 'BO' THEN 'Bolivia'
                    WHEN 'BA' THEN 'Bosnia And Herzegovina'
                    WHEN 'BW' THEN 'Botswana'
                    WHEN 'BV' THEN 'Bouvet Island'
                    WHEN 'BR' THEN 'Brazil'
                    WHEN 'IO' THEN 'British Indian Ocean Territory'
                    WHEN 'BN' THEN 'Brunei Darussalam'
                    WHEN 'BQ' THEN 'Bonaire, Saba, Sint Eustatius'
                    WHEN 'BG' THEN 'Bulgaria'
                    WHEN 'BF' THEN 'Burkina Faso'
                    WHEN 'BI' THEN 'Burundi'
                    WHEN 'KH' THEN 'Cambodia'
                    WHEN 'CM' THEN 'Cameroon'
                    WHEN 'CA' THEN 'Canada'
                    WHEN 'CV' THEN 'Cape Verde'
                    WHEN 'KY' THEN 'Cayman Islands'
                    WHEN 'CF' THEN 'Central African Republic'
                    WHEN 'TD' THEN 'Chad'
                    WHEN 'CL' THEN 'Chile'
                    WHEN 'CN' THEN 'China'
                    WHEN 'CX' THEN 'Christmas Island'
                    WHEN 'CC' THEN 'Cocos (Keeling) Islands'
                    WHEN 'CO' THEN 'Colombia'
                    WHEN 'KM' THEN 'Comoros'
                    WHEN 'CG' THEN 'Congo'
                    WHEN 'CD' THEN 'Congo, Democratic Republic'
                    WHEN 'CK' THEN 'Cook Islands'
                    WHEN 'CR' THEN 'Costa Rica'
                    WHEN 'CI' THEN 'Cote D\' Ivoire'
                    WHEN 'HR' THEN 'Croatia'
                    WHEN 'CU' THEN 'Cuba'
                    WHEN 'CY' THEN 'Cyprus'
                    WHEN 'CZ' THEN 'Czech Republic'
                    WHEN 'DK' THEN 'Denmark'
                    WHEN 'DJ' THEN 'Djibouti'
                    WHEN 'DM' THEN 'Dominica'
                    WHEN 'DO' THEN 'Dominican Republic'
                    WHEN 'EC' THEN 'Ecuador'
                    WHEN 'EG' THEN 'Egypt'
                    WHEN 'SV' THEN 'El Salvador'
                    WHEN 'GQ' THEN 'Equatorial Guinea'
                    WHEN 'ER' THEN 'Eritrea'
                    WHEN 'EE' THEN 'Estonia'
                    WHEN 'ET' THEN 'Ethiopia'
                    WHEN 'FK' THEN 'Falkland Islands (Malvinas)'
                    WHEN 'FO' THEN 'Faroe Islands'
                    WHEN 'FJ' THEN 'Fiji'
                    WHEN 'FI' THEN 'Finland'
                    WHEN 'FR' THEN 'France'
                    WHEN 'GF' THEN 'French Guiana'
                    WHEN 'PF' THEN 'French Polynesia'
                    WHEN 'TF' THEN 'French Southern Territories'
                    WHEN 'GA' THEN 'Gabon'
                    WHEN 'GM' THEN 'Gambia'
                    WHEN 'GE' THEN 'Georgia'
                    WHEN 'DE' THEN 'Germany'
                    WHEN 'GH' THEN 'Ghana'
                    WHEN 'GI' THEN 'Gibraltar'
                    WHEN 'GR' THEN 'Greece'
                    WHEN 'GL' THEN 'Greenland'
                    WHEN 'GD' THEN 'Grenada'
                    WHEN 'GP' THEN 'Guadeloupe'
                    WHEN 'GU' THEN 'Guam'
                    WHEN 'GT' THEN 'Guatemala'
                    WHEN 'GG' THEN 'Guernsey'
                    WHEN 'GN' THEN 'Guinea'
                    WHEN 'GW' THEN 'Guinea-Bissau'
                    WHEN 'GY' THEN 'Guyana'
                    WHEN 'HT' THEN 'Haiti'
                    WHEN 'HM' THEN 'Heard Island & Mcdonald Islands'
                    WHEN 'VA' THEN 'Holy See (Vatican City State)'
                    WHEN 'HN' THEN 'Honduras'
                    WHEN 'HK' THEN 'Hong Kong'
                    WHEN 'HU' THEN 'Hungary'
                    WHEN 'IS' THEN 'Iceland'
                    WHEN 'IN' THEN 'India'
                    WHEN 'ID' THEN 'Indonesia'
                    WHEN 'IR' THEN 'Iran'
                    WHEN 'IQ' THEN 'Iraq'
                    WHEN 'IE' THEN 'Ireland'
                    WHEN 'IM' THEN 'Isle Of Man'
                    WHEN 'IL' THEN 'Israel'
                    WHEN 'IT' THEN 'Italy'
                    WHEN 'JM' THEN 'Jamaica'
                    WHEN 'JP' THEN 'Japan'
                    WHEN 'JE' THEN 'Jersey'
                    WHEN 'JO' THEN 'Jordan'
                    WHEN 'KZ' THEN 'Kazakhstan'
                    WHEN 'KE' THEN 'Kenya'
                    WHEN 'KI' THEN 'Kiribati'
                    WHEN 'KR' THEN 'Korea'
                    WHEN 'KP' THEN 'North Korea'
                    WHEN 'KW' THEN 'Kuwait'
                    WHEN 'KG' THEN 'Kyrgyzstan'
                    WHEN 'LA' THEN 'Lao People\'s Democratic Republic'
                    WHEN 'LV' THEN 'Latvia'
                    WHEN 'LB' THEN 'Lebanon'
                    WHEN 'LS' THEN 'Lesotho'
                    WHEN 'LR' THEN 'Liberia'
                    WHEN 'LY' THEN 'Libyan Arab Jamahiriya'
                    WHEN 'LI' THEN 'Liechtenstein'
                    WHEN 'LT' THEN 'Lithuania'
                    WHEN 'LU' THEN 'Luxembourg'
                    WHEN 'MO' THEN 'Macao'
                    WHEN 'MK' THEN 'Macedonia'
                    WHEN 'MG' THEN 'Madagascar'
                    WHEN 'MW' THEN 'Malawi'
                    WHEN 'MY' THEN 'Malaysia'
                    WHEN 'MV' THEN 'Maldives'
                    WHEN 'ML' THEN 'Mali'
                    WHEN 'MT' THEN 'Malta'
                    WHEN 'MH' THEN 'Marshall Islands'
                    WHEN 'MQ' THEN 'Martinique'
                    WHEN 'MR' THEN 'Mauritania'
                    WHEN 'MU' THEN 'Mauritius'
                    WHEN 'YT' THEN 'Mayotte'
                    WHEN 'MX' THEN 'Mexico'
                    WHEN 'FM' THEN 'Micronesia, Federated States Of'
                    WHEN 'MD' THEN 'Moldova'
                    WHEN 'MC' THEN 'Monaco'
                    WHEN 'MN' THEN 'Mongolia'
                    WHEN 'ME' THEN 'Montenegro'
                    WHEN 'MS' THEN 'Montserrat'
                    WHEN 'MA' THEN 'Morocco'
                    WHEN 'MZ' THEN 'Mozambique'
                    WHEN 'MM' THEN 'Myanmar'
                    WHEN 'NA' THEN 'Namibia'
                    WHEN 'NR' THEN 'Nauru'
                    WHEN 'NP' THEN 'Nepal'
                    WHEN 'NL' THEN 'Netherlands'
                    WHEN 'AN' THEN 'Netherlands Antilles'
                    WHEN 'NC' THEN 'New Caledonia'
                    WHEN 'NZ' THEN 'New Zealand'
                    WHEN 'NI' THEN 'Nicaragua'
                    WHEN 'NE' THEN 'Niger'
                    WHEN 'NG' THEN 'Nigeria'
                    WHEN 'NU' THEN 'Niue'
                    WHEN 'NF' THEN 'Norfolk Island'
                    WHEN 'MP' THEN 'Northern Mariana Islands'
                    WHEN 'NO' THEN 'Norway'
                    WHEN 'OM' THEN 'Oman'
                    WHEN 'PK' THEN 'Pakistan'
                    WHEN 'PW' THEN 'Palau'
                    WHEN 'PS' THEN 'Palestine'
                    WHEN 'PA' THEN 'Panama'
                    WHEN 'PG' THEN 'Papua New Guinea'
                    WHEN 'PY' THEN 'Paraguay'
                    WHEN 'PE' THEN 'Peru'
                    WHEN 'PH' THEN 'Philippines'
                    WHEN 'PN' THEN 'Pitcairn'
                    WHEN 'PL' THEN 'Poland'
                    WHEN 'PT' THEN 'Portugal'
                    WHEN 'PR' THEN 'Puerto Rico'
                    WHEN 'QA' THEN 'Qatar'
                    WHEN 'RE' THEN 'Reunion'
                    WHEN 'RO' THEN 'Romania'
                    WHEN 'RU' THEN 'Russian Federation'
                    WHEN 'RW' THEN 'Rwanda'
                    WHEN 'BL' THEN 'Saint Barthelemy'
                    WHEN 'SH' THEN 'Saint Helena'
                    WHEN 'KN' THEN 'Saint Kitts And Nevis'
                    WHEN 'LC' THEN 'Saint Lucia'
                    WHEN 'MF' THEN 'Saint Martin'
                    WHEN 'PM' THEN 'Saint Pierre And Miquelon'
                    WHEN 'VC' THEN 'Saint Vincent And Grenadines'
                    WHEN 'WS' THEN 'Samoa'
                    WHEN 'SM' THEN 'San Marino'
                    WHEN 'ST' THEN 'Sao Tome And Principe'
                    WHEN 'SA' THEN 'Saudi Arabia'
                    WHEN 'SN' THEN 'Senegal'
                    WHEN 'RS' THEN 'Serbia'
                    WHEN 'SC' THEN 'Seychelles'
                    WHEN 'SL' THEN 'Sierra Leone'
                    WHEN 'SG' THEN 'Singapore'
                    WHEN 'SK' THEN 'Slovakia'
                    WHEN 'SI' THEN 'Slovenia'
                    WHEN 'SB' THEN 'Solomon Islands'
                    WHEN 'SO' THEN 'Somalia'
                    WHEN 'ZA' THEN 'South Africa'
                    WHEN 'GS' THEN 'South Georgia And Sandwich Isl.'
                    WHEN 'SS' THEN 'South Sudan'
                    WHEN 'ES' THEN 'Spain'
                    WHEN 'LK' THEN 'Sri Lanka'
                    WHEN 'SD' THEN 'Sudan'
                    WHEN 'SR' THEN 'Suriname'
                    WHEN 'SJ' THEN 'Svalbard And Jan Mayen'
                    WHEN 'SZ' THEN 'Swaziland'
                    WHEN 'SE' THEN 'Sweden'
                    WHEN 'CH' THEN 'Switzerland'
                    WHEN 'SY' THEN 'Syrian Arab Republic'
                    WHEN 'TW' THEN 'Taiwan'
                    WHEN 'TJ' THEN 'Tajikistan'
                    WHEN 'TZ' THEN 'Tanzania'
                    WHEN 'TH' THEN 'Thailand'
                    WHEN 'TL' THEN 'Timor-Leste'
                    WHEN 'TG' THEN 'Togo'
                    WHEN 'TK' THEN 'Tokelau'
                    WHEN 'TO' THEN 'Tonga'
                    WHEN 'TT' THEN 'Trinidad And Tobago'
                    WHEN 'TN' THEN 'Tunisia'
                    WHEN 'TR' THEN 'Turkey'
                    WHEN 'TM' THEN 'Turkmenistan'
                    WHEN 'TC' THEN 'Turks And Caicos Islands'
                    WHEN 'TV' THEN 'Tuvalu'
                    WHEN 'UG' THEN 'Uganda'
                    WHEN 'UA' THEN 'Ukraine'
                    WHEN 'AE' THEN 'United Arab Emirates'
                    WHEN 'GB' THEN 'United Kingdom'
                    WHEN 'US' THEN 'United States'
                    WHEN 'UM' THEN 'United States Outlying Islands'
                    WHEN 'UY' THEN 'Uruguay'
                    WHEN 'UZ' THEN 'Uzbekistan'
                    WHEN 'VU' THEN 'Vanuatu'
                    WHEN 'VE' THEN 'Venezuela'
                    WHEN 'VN' THEN 'Vietnam'
                    WHEN 'VG' THEN 'Virgin Islands, British'
                    WHEN 'VI' THEN 'Virgin Islands, U.S.'
                    WHEN 'WF' THEN 'Wallis And Futuna'
                    WHEN 'EH' THEN 'Western Sahara'
                    WHEN 'YE' THEN 'Yemen'
                    WHEN 'ZM' THEN 'Zambia'
                    WHEN 'ZW' THEN 'Zimbabwe'
                    ELSE ''
                END AS country_name";

}
