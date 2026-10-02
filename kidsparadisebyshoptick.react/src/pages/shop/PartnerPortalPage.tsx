import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowDownLeft,
  ArrowUpRight,
  BookOpen,
  Check,
  Copy,
  Eye,
  Link2,
  Loader2,
  MessageCircle,
  RefreshCcw,
  Search,
  ShieldCheck,
  Wallet,
} from 'lucide-react';
import { api, type AffiliateLedgerEntry } from '@/api/client';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { SeoHead } from '@/components/seo/SeoHead';
import { PAGE_SEO, SITE } from '@/lib/seo';
import { formatPrice } from '@/lib/utils';
import {
  buildAffiliateLedgerWhatsAppUrl,
  buildAffiliateSupportWhatsAppUrl,
} from '@/lib/whatsapp';

type EntryFilter = 'All' | 'Commission' | 'Payment' | 'Reversal';

function formatWhen(iso: string) {
  try {
    return new Date(iso).toLocaleString('en-PK', {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  } catch {
    return iso;
  }
}

function entryTone(type: string) {
  const t = type.toLowerCase();
  if (t === 'commission') return {
    badge: 'bg-emerald-50 text-emerald-800 border-emerald-100',
    amount: 'text-emerald-700',
    icon: ArrowDownLeft,
    sign: '+',
  };
  if (t === 'payment') return {
    badge: 'bg-sky-50 text-sky-800 border-sky-100',
    amount: 'text-sky-700',
    icon: ArrowUpRight,
    sign: '−',
  };
  return {
    badge: 'bg-amber-50 text-amber-900 border-amber-100',
    amount: 'text-amber-800',
    icon: RefreshCcw,
    sign: '−',
  };
}

function StatCard({
  label,
  value,
  hint,
  accent,
}: {
  label: string;
  value: string;
  hint: string;
  accent: string;
}) {
  return (
    <div className={`rounded-2xl border bg-white/90 p-5 shadow-sm ${accent}`}>
      <p className="text-xs font-semibold uppercase tracking-[0.14em] text-slate-500">{label}</p>
      <p className="mt-2 text-2xl font-bold tabular-nums text-slate-900 sm:text-3xl">{value}</p>
      <p className="mt-1 text-sm text-slate-500">{hint}</p>
    </div>
  );
}

function buildAffiliateLink(code: string) {
  return `${SITE.url}/shop?aff=${encodeURIComponent(code)}`;
}

function EntryRow({ entry }: { entry: AffiliateLedgerEntry }) {
  const tone = entryTone(entry.type);
  const Icon = tone.icon;
  return (
    <article className="grid gap-3 border-b border-slate-100 px-4 py-4 last:border-0 sm:grid-cols-[1.4fr_1fr_auto] sm:items-center sm:px-5">
      <div className="flex items-start gap-3 min-w-0">
        <div className={`mt-0.5 rounded-xl border p-2 ${tone.badge}`}>
          <Icon className="h-4 w-4" />
        </div>
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <span className={`rounded-full border px-2.5 py-0.5 text-[11px] font-semibold uppercase tracking-wide ${tone.badge}`}>
              {entry.type}
            </span>
            {entry.orderNumber && (
              <span className="text-xs font-medium text-slate-500">Order {entry.orderNumber}</span>
            )}
          </div>
          <p className="mt-1 text-sm text-slate-700 break-words">
            {entry.description || 'Ledger entry'}
          </p>
        </div>
      </div>
      <p className="text-sm text-slate-500 sm:text-right">{formatWhen(entry.createdAt)}</p>
      <p className={`text-lg font-bold tabular-nums sm:text-right ${tone.amount}`}>
        {tone.sign}{formatPrice(entry.amount)}
      </p>
    </article>
  );
}

export function PartnerPortalPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const codeFromUrl = (searchParams.get('code') || '').trim().toUpperCase();
  const [codeInput, setCodeInput] = useState(codeFromUrl);
  const [submittedCode, setSubmittedCode] = useState(codeFromUrl);
  const [typeFilter, setTypeFilter] = useState<EntryFilter>('All');
  const [linkCopied, setLinkCopied] = useState(false);

  useEffect(() => {
    setCodeInput(codeFromUrl);
    setSubmittedCode(codeFromUrl);
  }, [codeFromUrl]);

  const ledgerQuery = useQuery({
    queryKey: ['affiliate-ledger', submittedCode],
    queryFn: () => api.getAffiliateLedgerByCode(submittedCode),
    enabled: submittedCode.length >= 3,
    retry: false,
  });

  const filteredEntries = useMemo(() => {
    const entries = ledgerQuery.data?.entries ?? [];
    if (typeFilter === 'All') return entries;
    return entries.filter((e) => e.type.toLowerCase() === typeFilter.toLowerCase());
  }, [ledgerQuery.data?.entries, typeFilter]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const next = codeInput.trim().toUpperCase();
    setSubmittedCode(next);
    setTypeFilter('All');
    if (next) setSearchParams({ code: next }, { replace: true });
    else setSearchParams({}, { replace: true });
  };

  const partner = ledgerQuery.data?.partner;
  const affiliateLink = partner ? buildAffiliateLink(partner.code) : '';

  const copyAffiliateLink = async () => {
    if (!affiliateLink) return;
    try {
      await navigator.clipboard.writeText(affiliateLink);
      setLinkCopied(true);
      setTimeout(() => setLinkCopied(false), 2000);
    } catch {
      window.prompt('Copy your affiliate link:', affiliateLink);
    }
  };

  return (
    <div className="relative overflow-hidden">
      <SeoHead
        title={PAGE_SEO.partnerPortal.title}
        description={PAGE_SEO.partnerPortal.description}
        path={PAGE_SEO.partnerPortal.path}
        noIndex
      />

      <div className="pointer-events-none absolute inset-0 -z-10">
        <div className="absolute -left-24 top-0 h-72 w-72 rounded-full bg-teal-200/40 blur-3xl" />
        <div className="absolute right-0 top-20 h-80 w-80 rounded-full bg-sky-200/35 blur-3xl" />
        <div className="absolute bottom-0 left-1/3 h-64 w-64 rounded-full bg-brand-200/25 blur-3xl" />
      </div>

      <section className="mx-auto max-w-5xl px-4 pb-16 pt-10 sm:px-6 sm:pt-14">
        <div className="animate-fade-in max-w-2xl">
          <p className="inline-flex items-center gap-2 rounded-full border border-teal-200/80 bg-white/80 px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-teal-800 shadow-sm">
            <ShieldCheck className="h-3.5 w-3.5" />
            Partner portal · read only
          </p>
          <h1 className="mt-4 font-semibold tracking-tight text-slate-900 text-[clamp(1.85rem,4vw,2.75rem)] leading-tight">
            Kids Paradise
            <span className="block text-teal-700">Affiliate Ledger</span>
          </h1>
          <p className="mt-3 max-w-xl text-base leading-relaxed text-slate-600 sm:text-lg">
            Enter your partner code to view commissions, payments, and outstanding balance.
            This page is view-only — no changes can be made here.
          </p>
        </div>

        <form
          onSubmit={handleSearch}
          className="animate-fade-in mt-8 rounded-3xl border border-slate-200/80 bg-white/90 p-5 shadow-[0_20px_50px_-28px_rgba(15,23,42,0.35)] backdrop-blur sm:p-6"
          style={{ animationDelay: '80ms' }}
        >
          <label className="block text-sm font-semibold text-slate-800" htmlFor="partner-code">
            Partner code
          </label>
          <div className="mt-3 flex flex-col gap-3 sm:flex-row">
            <div className="relative flex-1">
              <Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
              <Input
                id="partner-code"
                value={codeInput}
                onChange={(e) => setCodeInput(e.target.value.toUpperCase())}
                placeholder="e.g. A1B2C3D4E5F6"
                className="pl-10 font-mono tracking-wider uppercase"
                autoComplete="off"
                spellCheck={false}
              />
            </div>
            <Button type="submit" size="lg" className="sm:min-w-[140px]" disabled={ledgerQuery.isFetching}>
              {ledgerQuery.isFetching ? (
                <>
                  <Loader2 className="h-4 w-4 animate-spin" /> Looking up…
                </>
              ) : (
                <>
                  <BookOpen className="h-4 w-4" /> View ledger
                </>
              )}
            </Button>
          </div>
          <p className="mt-3 flex items-start gap-2 text-xs text-slate-500">
            <Eye className="mt-0.5 h-3.5 w-3.5 shrink-0" />
            Use the exact code from your partner link. Filter results below after lookup.
          </p>
          <a
            href={buildAffiliateSupportWhatsAppUrl(codeInput || submittedCode)}
            target="_blank"
            rel="noopener noreferrer"
            className="mt-4 inline-flex w-full sm:w-auto items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-[#25D366] hover:bg-[#20bd5a] text-white text-sm font-semibold shadow-sm transition-all"
          >
            <MessageCircle className="h-4 w-4 fill-white" />
            WhatsApp Admin Support
          </a>
        </form>

        {submittedCode.length > 0 && ledgerQuery.isError && (
          <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-rose-800">
            <p className="font-semibold">No ledger found</p>
            <p className="mt-1 text-sm text-rose-700/90">
              Check the partner code and try again. Codes are case-insensitive.
            </p>
            <a
              href={buildAffiliateSupportWhatsAppUrl(submittedCode)}
              target="_blank"
              rel="noopener noreferrer"
              className="mt-3 inline-flex items-center justify-center gap-2 px-4 py-2 rounded-xl bg-[#25D366] hover:bg-[#20bd5a] text-white text-sm font-semibold"
            >
              <MessageCircle className="h-4 w-4 fill-white" />
              WhatsApp Admin about this code
            </a>
          </div>
        )}

        {partner && ledgerQuery.data && (
          <div className="mt-8 space-y-6 animate-fade-in">
            <div className="rounded-3xl border border-slate-200/80 bg-gradient-to-br from-slate-900 via-slate-900 to-teal-950 p-6 text-white shadow-xl sm:p-7">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-teal-200/90">Partner</p>
                  <h2 className="mt-1 text-2xl font-semibold tracking-tight">{partner.name}</h2>
                  <p className="mt-2 font-mono text-sm tracking-wider text-slate-300">{partner.code}</p>
                </div>
                <div className="rounded-full border border-white/15 bg-white/10 px-3 py-1 text-xs font-semibold uppercase tracking-wide">
                  {partner.isActive ? 'Active' : 'Inactive'}
                </div>
              </div>
              <p className="mt-5 max-w-2xl text-sm leading-relaxed text-slate-300">
                Formula: Commission − Reversals − Payments = Outstanding.
                Commission is 10% of product amount after discount (delivery excluded).
              </p>

              <div className="mt-6 rounded-2xl border border-white/10 bg-white/5 p-4">
                <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.14em] text-teal-200/90">
                  <Link2 className="h-3.5 w-3.5" />
                  Your affiliate link
                </div>
                <p className="mt-2 break-all font-mono text-sm text-slate-200">{affiliateLink}</p>
                <p className="mt-2 text-xs text-slate-400">
                  Share this link. Orders placed through it are attributed to you.
                </p>
                <div className="mt-4 flex flex-col gap-2 sm:flex-row">
                  <Button
                    type="button"
                    onClick={copyAffiliateLink}
                    className="w-full sm:w-auto bg-white text-slate-900 hover:bg-teal-50 border-0"
                  >
                    {linkCopied ? (
                      <>
                        <Check className="h-4 w-4 text-emerald-600" /> Link copied
                      </>
                    ) : (
                      <>
                        <Copy className="h-4 w-4" /> Copy my link
                      </>
                    )}
                  </Button>
                  <a
                    href={buildAffiliateLedgerWhatsAppUrl(ledgerQuery.data)}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex w-full sm:w-auto items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-[#25D366] hover:bg-[#20bd5a] text-white text-sm font-semibold shadow-sm transition-all"
                  >
                    <MessageCircle className="h-4 w-4 fill-white" />
                    WhatsApp Admin (with details)
                  </a>
                </div>
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <StatCard
                label="Earned"
                value={formatPrice(ledgerQuery.data.totalCommission)}
                hint="Net commission"
                accent="border-emerald-100"
              />
              <StatCard
                label="Paid"
                value={formatPrice(ledgerQuery.data.totalPaid)}
                hint="Settled to you"
                accent="border-sky-100"
              />
              <StatCard
                label="Outstanding"
                value={formatPrice(ledgerQuery.data.balance)}
                hint="Still payable"
                accent="border-teal-100"
              />
              <StatCard
                label="Orders"
                value={String(partner.attributedOrders)}
                hint="Attributed sales"
                accent="border-slate-200"
              />
            </div>

            <div className="overflow-hidden rounded-3xl border border-slate-200/80 bg-white/95 shadow-sm">
              <div className="flex flex-col gap-4 border-b border-slate-100 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex items-center gap-2">
                  <Wallet className="h-4 w-4 text-teal-700" />
                  <h3 className="font-semibold text-slate-900">Ledger entries</h3>
                  <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600">
                    {filteredEntries.length}
                  </span>
                </div>
                <div className="flex flex-wrap gap-2">
                  {(['All', 'Commission', 'Payment', 'Reversal'] as EntryFilter[]).map((f) => (
                    <button
                      key={f}
                      type="button"
                      onClick={() => setTypeFilter(f)}
                      className={`rounded-full px-3 py-1.5 text-xs font-semibold transition-colors ${
                        typeFilter === f
                          ? 'bg-slate-900 text-white'
                          : 'bg-slate-100 text-slate-600 hover:bg-slate-200'
                      }`}
                    >
                      {f}
                    </button>
                  ))}
                </div>
              </div>

              {filteredEntries.length === 0 ? (
                <div className="px-5 py-14 text-center">
                  <p className="font-semibold text-slate-700">No entries in this filter</p>
                  <p className="mt-1 text-sm text-slate-500">
                    Commission posts when an affiliated order is marked Delivered.
                  </p>
                </div>
              ) : (
                <div>
                  {filteredEntries.map((entry) => (
                    <EntryRow key={entry.id} entry={entry} />
                  ))}
                </div>
              )}
            </div>
          </div>
        )}

        {!submittedCode && (
          <div className="mt-10 rounded-3xl border border-dashed border-slate-300 bg-white/60 px-6 py-12 text-center">
            <BookOpen className="mx-auto h-8 w-8 text-slate-400" />
            <p className="mt-3 font-semibold text-slate-700">Search with your partner code</p>
            <p className="mx-auto mt-1 max-w-md text-sm text-slate-500">
              Your code is the opaque token in your affiliate link after <span className="font-mono">?aff=</span>.
            </p>
            <a
              href={buildAffiliateSupportWhatsAppUrl()}
              target="_blank"
              rel="noopener noreferrer"
              className="mt-5 inline-flex items-center justify-center gap-2 px-5 py-2.5 rounded-xl bg-[#25D366] hover:bg-[#20bd5a] text-white text-sm font-semibold"
            >
              <MessageCircle className="h-4 w-4 fill-white" />
              WhatsApp Admin Support
            </a>
          </div>
        )}
      </section>
    </div>
  );
}
