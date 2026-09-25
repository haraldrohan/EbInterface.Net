using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using EbInterface.Internal;
using EbInterface.Model;

namespace EbInterface.Validation
{
    /// <summary>
    /// Rechenprüfungen, wie sie der Test-Upload von e-Rechnung.gv.at durchführt (AF-0025/AF-0089, AF-0028, AF-0032,
    /// AF-0033, AF-0122; ermittelt am 25./26.09.2026). Sie stehen nicht auf der Regelseite; Toleranz und Rechenweg
    /// wurden mit synthetischen Testrechnungen bestimmt (siehe docs/pruefregeln.md).
    /// </summary>
    internal static partial class ERechnungRules
    {
        /// <summary>
        /// Größte Abweichung, die das Portal bei einem Betrag hinnimmt (0,10 angenommen, 0,12 abgelehnt). Bei Summen wächst sie
        /// um 0,10 je summiertem Posten (2 Zeilen: 0,20 angenommen, 0,25 abgelehnt; 1 Zeile: 0,15 abgelehnt).
        /// </summary>
        private const decimal AmountTolerance = 0.10m;

        private const decimal MaxRoundingAmount = 0.02m;

        // ERB-26 Zeilenbetrag, ERB-27 Zahlbetrag, ERB-28 Rundungsbetrag, ERB-29 Steuergrundlage, ERB-39 Steuerbetrag
        private static void CheckArithmetic(Context ctx)
        {
            EbInvoice invoice = InvoiceMapper.Map(ctx.Invoice, ctx.Version);
            List<LineItem> lines = invoice.AllLineItems.ToList();

            for (int i = 0; i < lines.Count && i < ctx.LineItems.Count; i++)
            {
                LineItem line = lines[i];
                decimal expected = ExpectedLineAmount(line);
                // Das Portal prüft netto (AF-0025/AF-0089) und brutto (AF-0026) mit derselben Toleranz.
                bool netDiffers = Differs(line.LineItemAmount, expected);
                bool grossDiffers = Differs(Gross(line.LineItemAmount, line.TaxPercent), Gross(expected, line.TaxPercent));
                if (netDiffers || grossDiffers)
                {
                    XElement at = ctx.LineItems[i].Element(ctx.Ns + "LineItemAmount") ?? ctx.LineItems[i];
                    ctx.Error("ERB-26", at,
                        $"Der Zeilenbetrag {Format(line.LineItemAmount)} passt nicht zu Menge × Einzelpreis" +
                        (line.BaseQuantity is decimal b && b != 1m ? " ÷ Preiseinheit" : string.Empty) +
                        (line.ReductionsAndSurcharges.Count > 0 ? " − Abschläge + Aufschläge" : string.Empty) +
                        $" (= {Format(expected)}).");
                }
            }

            CheckRoundingAmount(ctx, invoice);
            CheckPayableAmount(ctx, invoice, lines);
            CheckTaxSummary(ctx, invoice, lines);
        }

        /// <summary>Menge × Einzelpreis ÷ Preiseinheit − Abschläge + Aufschläge der Zeile.</summary>
        private static decimal ExpectedLineAmount(LineItem line)
        {
            decimal amount = line.Quantity * line.UnitPrice / (line.BaseQuantity is decimal b && b != 0m ? b : 1m);
            foreach (ReductionOrSurcharge item in line.ReductionsAndSurcharges)
            {
                if (item.Kind == ReductionOrSurchargeKind.Reduction) amount -= AmountOf(item);
                else if (item.Kind == ReductionOrSurchargeKind.Surcharge) amount += AmountOf(item);
            }

            return amount;
        }

        private static decimal AmountOf(ReductionOrSurcharge item) =>
            item.Amount ?? (item.BaseAmount ?? 0m) * (item.Percentage ?? 0m) / 100m;

        private static void CheckRoundingAmount(Context ctx, EbInvoice invoice)
        {
            if (invoice.RoundingAmount is decimal rounding && Math.Abs(rounding) > MaxRoundingAmount)
            {
                ctx.Error("ERB-28", ctx.Invoice.Element(ctx.Ns + "RoundingAmount") ?? ctx.Invoice,
                    $"Der Rundungsbetrag {Format(rounding)} muss zwischen -0,02 und 0,02 liegen.");
            }
        }

        /// <summary>
        /// Zahlbetrag = Summe der Zeilen brutto ± Auf-/Abschläge auf Rechnungsebene (brutto) + Below-The-Line-Beträge.
        /// Wie im Portal werden die Zeilen aus Menge × Preis berechnet, nicht aus dem angegebenen Zeilenbetrag.
        /// Eine Vorauszahlung (PrepaidAmount) wird vom Portal nicht abgezogen.
        /// </summary>
        private static void CheckPayableAmount(Context ctx, EbInvoice invoice, List<LineItem> lines)
        {
            decimal expected = lines.Sum(l => Gross(ExpectedLineAmount(l), l.TaxPercent));
            foreach (ReductionOrSurcharge item in invoice.ReductionsAndSurcharges)
            {
                decimal gross = Gross(AmountOf(item), item.TaxPercent ?? 0m);
                if (item.Kind == ReductionOrSurchargeKind.Reduction) expected -= gross;
                else if (item.Kind == ReductionOrSurchargeKind.Surcharge) expected += gross;
            }

            expected += invoice.BelowTheLineItems.Sum(b => b.LineItemAmount);
            expected += invoice.RoundingAmount ?? 0m;

            int summed = Math.Max(1, lines.Count + invoice.ReductionsAndSurcharges.Count);
            if (Differs(invoice.PayableAmount, expected, summed))
            {
                ctx.Error("ERB-27", ctx.Invoice.Element(ctx.Ns + "PayableAmount") ?? ctx.Invoice,
                    $"Der Zahlbetrag {Format(invoice.PayableAmount)} passt nicht zur Summe der Zeilen brutto " +
                    $"± Auf-/Abschläge + Below-The-Line-Beträge (= {Format(expected)}). " +
                    "Eine Vorauszahlung (PrepaidAmount) wird von e-Rechnung.gv.at nicht abgezogen.");
            }
        }

        /// <summary>Steuergrundlage je Steuersatz = Summe der Zeilen ± Auf-/Abschläge; Steuerbetrag = Grundlage × Satz.</summary>
        private static void CheckTaxSummary(Context ctx, EbInvoice invoice, List<LineItem> lines)
        {
            List<XElement> taxElements = (ctx.Version == EbInterfaceVersion.V4p3
                ? ctx.Invoice.Element(ctx.Ns + "Tax")?.Element(ctx.Ns + "VAT")?.Elements(ctx.Ns + "VATItem")
                : ctx.Invoice.Element(ctx.Ns + "Tax")?.Elements(ctx.Ns + "TaxItem"))?.ToList() ?? new List<XElement>();

            for (int i = 0; i < invoice.TaxItems.Count && i < taxElements.Count; i++)
            {
                TaxItem tax = invoice.TaxItems[i];

                decimal expectedBase = lines.Where(l => l.TaxPercent == tax.TaxPercent).Sum(ExpectedLineAmount);
                foreach (ReductionOrSurcharge item in invoice.ReductionsAndSurcharges.Where(r => r.TaxPercent == tax.TaxPercent))
                {
                    if (item.Kind == ReductionOrSurchargeKind.Reduction) expectedBase -= AmountOf(item);
                    else if (item.Kind == ReductionOrSurchargeKind.Surcharge) expectedBase += AmountOf(item);
                }

                // Mehrere Steuerzeilen mit gleichem Satz (z. B. unterschiedliche Kategorien) nicht auseinanderrechnen.
                bool unique = invoice.TaxItems.Count(t => t.TaxPercent == tax.TaxPercent) == 1;
                int summed = Math.Max(1, lines.Count(l => l.TaxPercent == tax.TaxPercent) +
                    invoice.ReductionsAndSurcharges.Count(r => r.TaxPercent == tax.TaxPercent));
                if (unique && Differs(tax.TaxableAmount, expectedBase, summed))
                {
                    ctx.Error("ERB-29", taxElements[i],
                        $"Die Steuergrundlage {Format(tax.TaxableAmount)} für {Format(tax.TaxPercent)} % passt nicht zur Summe " +
                        $"der Zeilen mit diesem Steuersatz ± Auf-/Abschläge (= {Format(expectedBase)}).");
                }

                // Wie im Portal (AF-0033): Steuerbetrag gegen den Satz auf die aus den Zeilen berechnete Grundlage.
                decimal taxBase = unique ? expectedBase : tax.TaxableAmount;
                decimal expectedTax = taxBase * tax.TaxPercent / 100m;
                if (tax.TaxAmount is decimal amount && Differs(amount, expectedTax))
                {
                    ctx.Error("ERB-39", taxElements[i],
                        $"Der Steuerbetrag {Format(amount)} ist nicht {Format(tax.TaxPercent)} % der Steuergrundlage " +
                        $"{Format(taxBase)} (= {Format(expectedTax)}).");
                }
            }
        }

        private static decimal Gross(decimal net, decimal taxPercent) => net * (1m + taxPercent / 100m);

        private static bool Differs(decimal actual, decimal expected, int summedItems = 1) =>
            Math.Abs(actual - expected) > AmountTolerance * summedItems;

        private static string Format(decimal value) =>
            Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.GetCultureInfo("de-AT"));
    }
}
