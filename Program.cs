using System;
using System.Globalization;
using System.Text;
using System.Xml; 

foreach (var file in Directory.EnumerateFiles(Directory.GetCurrentDirectory())) {
    if (!file.ToLower().EndsWith(".xml"))
        continue;

    var doc = new XmlDocument();

    doc.Load(file);
    var acc = doc.DocumentElement.Query("Stmt > Acct > Id > IBAN");
    var records = new List<string>();
    records.Add("Könyvelés dátuma;Értéknap;Számlaszám;Partner neve;Partner számlaszáma;Összeg;Terhelés vagy jóváírás (T/J);Tranzakció típusa;Közlemény");
    foreach (var element in doc.DocumentElement.GetElementsByTagName("Stmt").Cast<XmlElement>())
    {
        var entries = element.GetElementsByTagName("Ntry").Cast<XmlElement>();
        foreach (var ntry in entries)
        {
            var transaction = new Transaction(ntry);
            bool credit = transaction.Kind == "CRDT";
            string? partner;
            string? partnerAcc;
            if (credit) {
                partner = transaction.Debitor;
                partnerAcc = transaction.DebitorAccount;
            } else  {
                partner = transaction.Creditor;
                partnerAcc = transaction.CreditorAccount;
            }
            if (partner == null) {
                var text = transaction.AdditionalInfo;
                var lines = text?.Split("\n");
                if (lines?.Count() > 2 ) {
                    partner = lines[2];
                }
            }
            var amount = transaction.Amount;
            var iamount = (int)float.Parse(amount, CultureInfo.InvariantCulture);
            if (!credit)
                iamount *= -1;
            var type = ntry.Query("BkTxCd > Prtry > Cd");
            var tj = credit ? "J" : "T";
            var comment = transaction.Message;

            records.Add($"{transaction.BookingDate}; {transaction.ValueDate}; {acc}; {partner}; {partnerAcc}; {iamount}; { tj }; {type}; {comment}");
        }
    }

    File.WriteAllLines($"{file.Split(".").First()}.csv", records, Encoding.UTF8);

}

struct Transaction {
    public string? Kind { get; set; }
    public string? BookingDate { get; set; }
    public string? ValueDate { get; set; }
    public string? Creditor { get; set; }
    public string? CreditorAccount { get; set; }
    public string? Debitor { get; set; }
    public string? DebitorAccount { get; set; }
    public string? Amount { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? Type { get; set; }
    public string? Message { get; set; }

    public Transaction(XmlElement ntry) {
        Kind = ntry.Query("CdtDbtInd");
        BookingDate = ntry.Query("BookgDt > Dt");
        ValueDate = ntry.Query("ValDt > Dt");
        Creditor = ntry.Query("NtryDtls > RltdPties > Cdtr > Nm");
        CreditorAccount = ntry.Query("NtryDtls > RltdPties > CdtrAcct > Id > IBAN");
        Debitor = ntry.Query("NtryDtls > RltdPties > Dbtr > Nm");
        DebitorAccount = ntry.Query("NtryDtls > RltdPties > DbtrAcct > Id > IBAN");
        Amount = ntry.Query("Amt");
        AdditionalInfo = ntry.Query("NtryDtls > TxDtls > AddtlTxInf");
        Type = ntry.Query("BkTxCd > Prtry > Cd");
        Message = ntry.Query("NtryDtls > RmtInf > Ustrd");
    }
}

static class XmlElementExt
{
    
    public static XmlElement? Get(this XmlElement? element, string tag)
    {
        return element?.GetElementsByTagName(tag).Cast<XmlElement>().FirstOrDefault();
    }
    
    public static XmlElement? Get(this XmlElement? element, List<string> tags)
    {
        XmlElement? result = element;
        foreach (var tag in tags) {
            result = result.Get(tag);
            if (result == null)
                break;
        }
        return result;
    }

    public static string? Query(this XmlElement? element, string query) {
        return Get(element, query.Split('>').Select(p => p.Trim()).ToList())?.InnerXml;
    }
}