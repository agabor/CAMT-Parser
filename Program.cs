using System;
using System.Globalization;
using System.Text;
using System.Xml; 

foreach (var file in Directory.EnumerateFiles(Directory.GetCurrentDirectory())) {
    if (!file.ToLower().EndsWith(".xml"))
        continue;

    var doc = new XmlDocument();

    doc.Load(file);
    var acc = doc.DocumentElement.Get("Stmt").Get("Acct").Get("Id").Get("IBAN")?.InnerXml;
    var records = new List<string>();
    records.Add("Könyvelés dátuma;Értéknap;Számlaszám;Partner neve;Partner számlaszáma;Összeg;Terhelés vagy jóváírás (T/J);Tranzakció típusa;Közlemény");
    foreach (var element in doc.DocumentElement.GetElementsByTagName("Stmt").Cast<XmlElement>())
    {
        var entries = element.GetElementsByTagName("Ntry").Cast<XmlElement>();
        foreach (var ntry in entries)
        {
            bool credit = ntry.Get("CdtDbtInd")?.InnerText == "CRDT";
            var bookDate = ntry.Get("BookgDt").Get("Dt")?.InnerXml;
            var valDate = ntry.Get("ValDt").Get("Dt")?.InnerXml;
            string? partner;
            string? partnerAcc;
            if (credit) {
                partner = ntry.Get("NtryDtls").Get("RltdPties").Get("Dbtr").Get("Nm")?.InnerXml;
                partnerAcc = ntry.Get("NtryDtls").Get("RltdPties").Get("DbtrAcct").Get("Id").Get("IBAN")?.InnerXml;
            } else  {
                partner = ntry.Get("NtryDtls").Get("RltdPties").Get("Cdtr").Get("Nm")?.InnerXml;
                partnerAcc = ntry.Get("NtryDtls").Get("RltdPties").Get("CdtrAcct").Get("Id").Get("IBAN")?.InnerXml;
            }
            if (partner == null) {
                var text = ntry.Get("NtryDtls").Get("TxDtls").Get("AddtlTxInf")?.InnerXml;
                var lines = text?.Split("\n");
                if (lines?.Count() > 2 ) {
                    partner = lines[2];
                }
            }
            var amount = ntry.Get("Amt")?.InnerXml;
            var iamount = (int)float.Parse(amount, CultureInfo.InvariantCulture);
            if (!credit)
                iamount *= -1;
            var type = ntry.Get("BkTxCd").Get("Prtry").Get("Cd")?.InnerXml;
            var tj = credit ? "J" : "T";
            var comment = ntry.Get("NtryDtls").Get("RmtInf").Get("Ustrd")?.InnerXml;

            records.Add($"{bookDate}; {valDate}; {acc}; {partner}; {partnerAcc}; {iamount}; { tj }; {type}; {comment}");
        }
    }

    File.WriteAllLines($"{file.Split(".").First()}.csv", records, Encoding.UTF8);

}
static class XmlElementExt
{
    
    public static XmlElement? Get(this XmlElement? element, string tag)
    {
        return element?.GetElementsByTagName(tag).Cast<XmlElement>().FirstOrDefault();
    }
}