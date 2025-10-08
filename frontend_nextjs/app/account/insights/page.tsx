"use client";

import { motion } from "framer-motion";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import Link from "next/link";
import AccessDenied from "@/components/AccessDenied";
import {
  ArrowLeft,
  BarChart3,
  Users,
  MessageSquare,
  Activity,
  TrendingUp,
  Calendar,
  Clock,
  Heart,
  Eye
} from "lucide-react";
import { useUser } from "@/contexts/UserContext";
import { useGet } from '@/hooks/useApi';
import { apiService } from '@/hooks/apiService';
import { useEffect, useMemo, useState } from 'react';
import { Input } from '@/components/ui/input';
import { Loader2 } from 'lucide-react';

const containerVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.6,
      staggerChildren: 0.1
    }
  }
};

const itemVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: { opacity: 1, y: 0 }
};

export default function InsightsPage() {
  const { user, isAuthenticated } = useUser();

  // Hooks must be called unconditionally — place them before any early returns
  // Fetch user's links to display link count in the overview
  const { data: linksResp, loading: loadingLinks } = useGet<any>('/link');
  // Fetch per-link click counts so we can display total clicks in the overview
  const { data: linkCountsResp, loading: loadingLinkCounts } = useGet<any[]>('/analytics/my/links');

  const [daysActive, setDaysActive] = useState<number | null>(null);

  useEffect(() => {
    // Try to derive createdAt from user object if present
    const maybe = (user as any)?.createdAt || (user as any)?.created_at || (user as any)?.created;
    if (maybe) {
      const d = new Date(maybe);
      if (!isNaN(d.getTime())) {
        const now = new Date();
        const days = Math.floor((now.getTime() - d.getTime()) / (1000 * 60 * 60 * 24)) + 1;
        setDaysActive(days > 0 ? days : 1);
        return;
      }
    }

    // Fallback: fetch /profile to obtain createdAt
    let mounted = true;
    (async () => {
      try {
        const p = await apiService.get('/profile');
        const created = p?.data?.data?.createdAt || p?.data?.createdAt || p?.data?.data?.created_at;
        if (mounted && created) {
          const d = new Date(created);
          if (!isNaN(d.getTime())) {
            const now = new Date();
            const days = Math.floor((now.getTime() - d.getTime()) / (1000 * 60 * 60 * 24)) + 1;
            setDaysActive(days > 0 ? days : 1);
            return;
          }
        }
      } catch {
        // ignore
      }
      if (mounted) setDaysActive(1);
    })();

    return () => { mounted = false; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (!isAuthenticated || !user) {
    return <AccessDenied />;
  }

  // Compute links count from the /link response
  const linksCount = (() => {
    try {
      const resp = (linksResp && (linksResp as any).data) ? (linksResp as any).data : linksResp ?? {};
      const groups = (resp && resp.groups) ? resp.groups : (resp?.groups ?? []);
      const ungrouped = (resp && resp.ungrouped) ? resp.ungrouped : (resp?.ungrouped ?? { links: [] });
      const links = ([] as any[]).concat(...(groups.map((g: any) => g.links ?? [])), ungrouped.links ?? []);
      return links.length;
    } catch {
      return 0;
    }
  })();

  const totalClicks = (() => {
    try {
      const counts = (linkCountsResp && (linkCountsResp as any).data) ? (linkCountsResp as any).data : linkCountsResp ?? [];
      return (counts as any[]).reduce((s, r) => s + (r?.clicks || 0), 0);
    } catch {
      return 0;
    }
  })();

  // parsed counts array for passing into the overview component
  const parsedLinkCounts = (linkCountsResp && (linkCountsResp as any).data) ? (linkCountsResp as any).data : (Array.isArray(linkCountsResp) ? linkCountsResp : []);


  return (
    <div className="min-h-screen bg-background">
      <div className="container mx-auto px-4 py-8 max-w-6xl">
        <motion.div
          variants={containerVariants}
          initial="hidden"
          animate="visible"
          className="space-y-8"
        >
          {/* Back Navigation */}
          <motion.div variants={itemVariants}>
            <Link
              href="/account/profile"
              className="inline-flex items-center text-sm text-muted-foreground hover:text-foreground transition-colors"
            >
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Profile
            </Link>
          </motion.div>

          {/* Header */}
          <motion.div variants={itemVariants} className="space-y-2">
            <div className="flex items-center space-x-3">
              <BarChart3 className="h-8 w-8 text-primary" />
              <h1 className="text-3xl font-bold">Account Insights</h1>
            </div>
            <p className="text-muted-foreground">
              Track your activity, engagement, and community presence on linqyard
            </p>
          </motion.div>

          {/* Quick Stats Overview */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <Activity className="h-5 w-5 mr-2" />
                  Activity Overview
                </CardTitle>
                <CardDescription>
                  Your account statistics and activity summary
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="text-center p-6 rounded-lg border bg-gradient-to-br from-blue-50 to-blue-100 dark:from-blue-900/20 dark:to-blue-800/20">
                    <MessageSquare className="h-8 w-8 text-blue-600 mx-auto mb-2" />
                    <div className="text-2xl font-bold text-blue-700 dark:text-blue-300">
                      {loadingLinks ? '...' : linksCount}
                    </div>
                    <div className="text-sm text-blue-600 dark:text-blue-400">Links</div>
                  </div>
                  {/* Total Clicks */}
                  <div className="text-center p-6 rounded-lg border bg-gradient-to-br from-purple-50 to-purple-100 dark:from-purple-900/20 dark:to-purple-800/20">
                    <Heart className="h-8 w-8 text-purple-600 mx-auto mb-2" />
                    <div className="text-2xl font-bold text-purple-700 dark:text-purple-300">
                      {loadingLinkCounts ? '...' : totalClicks}
                    </div>
                    <div className="text-sm text-purple-600 dark:text-purple-400">Total Clicks</div>

                    {/* Top link breakdown (up to 3) moved here */}
                    {(!loadingLinkCounts && parsedLinkCounts && parsedLinkCounts.length > 0) && (() => {
                      try {
                        const resp = (linksResp && (linksResp as any).data) ? (linksResp as any).data : linksResp ?? {};
                        const groups = (resp && resp.groups) ? resp.groups : (resp?.groups ?? []);
                        const ungrouped = (resp && resp.ungrouped) ? resp.ungrouped : (resp?.ungrouped ?? { links: [] });
                        const linksArr = ([] as any[]).concat(...(groups.map((g: any) => g.links ?? [])), ungrouped.links ?? []);

                        const mapped = (parsedLinkCounts as any[])
                          .map((c) => {
                            const link = linksArr.find((l: any) => l.id === c.linkId);
                            return { name: link ? link.name : c.linkId, clicks: c.clicks };
                          })
                          .sort((a, b) => (b.clicks || 0) - (a.clicks || 0))
                          .slice(0, 3);

                        return (
                          <div className="mt-3 text-left text-xs space-y-1">
                            {mapped.map((m) => (
                              <div key={m.name} className="flex items-center justify-between">
                                <span className="truncate text-muted-foreground">{m.name}</span>
                                <span className="font-medium">{m.clicks}</span>
                              </div>
                            ))}
                          </div>
                        );
                      } catch {
                        return null;
                      }
                    })()}
                  </div>

                  <div className="text-center p-6 rounded-lg border bg-gradient-to-br from-green-50 to-green-100 dark:from-green-900/20 dark:to-green-800/20">
                    <Users className="h-8 w-8 text-green-600 mx-auto mb-2" />
                    <div className="text-2xl font-bold text-green-700 dark:text-green-300">0</div>
                    <div className="text-sm text-green-600 dark:text-green-400">Friends</div>
                  </div>

                  <div className="text-center p-6 rounded-lg border bg-gradient-to-br from-orange-50 to-orange-100 dark:from-orange-900/20 dark:to-orange-800/20">
                    <Calendar className="h-8 w-8 text-orange-600 mx-auto mb-2" />
                    <div className="text-2xl font-bold text-orange-700 dark:text-orange-300">{daysActive ?? '...'}</div>
                    <div className="text-sm text-orange-600 dark:text-orange-400">Days Active</div>
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>

          {/* Engagement Metrics */}

          {/* Action Items */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle>Get More Engaged</CardTitle>
                <CardDescription>
                  Tips to increase your activity and build connections
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="p-4 rounded-lg border bg-muted/50">
                    <h4 className="font-medium mb-2">Complete Your Profile</h4>
                    <p className="text-sm text-muted-foreground mb-3">
                      Add a bio and profile picture to make a great first impression.
                    </p>
                    <Link href="/account/profile">
                      <Button size="sm" variant="outline">
                        Edit Profile
                      </Button>
                    </Link>
                  </div>
                  <div className="p-4 rounded-lg border bg-muted/50">
                    <h4 className="font-medium mb-2">Share Your First Post</h4>
                    <p className="text-sm text-muted-foreground mb-3">
                      Start engaging with the community by sharing your thoughts.
                    </p>
                    <Button size="sm" variant="outline" disabled>
                      Create Post (Coming Soon)
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>

          {/* Analytics Overview */}
          <motion.div variants={itemVariants}>
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center">
                  <BarChart3 className="h-5 w-5 mr-2" />
                  Link Clicks
                </CardTitle>
                <CardDescription>
                  Total clicks across your links and per-link breakdown
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="mt-6">
                  <DeviceUsage />
                </div>
                <div className="mt-6">
                  <AnalyticsOverview countsProp={parsedLinkCounts} />
                </div>
                <div className="mt-6">
                  <MonthlyStats />
                </div>
              </CardContent>
            </Card>
          </motion.div>
        </motion.div>
      </div>
    </div>
  );
}

function AnalyticsOverview({ countsProp }: { countsProp?: Array<{ linkId: string; clicks: number }> }) {
  // GET /analytics/my/links -> [{ linkId, clicks }]
  const { data: countsResp, loading: loadingCounts, error: countsError, refetch: refetchCounts } = useGet<any[]>("/analytics/my/links");

  // GET /link -> to map linkId to name (owner's links)
  const { data: linksResp, loading: loadingLinks, error: linksError, refetch: refetchLinks } = useGet<any>("/link");

  const [rows, setRows] = useState<Array<{ linkId: string; name: string; clicks: number }>>([]);

  useEffect(() => {
    const countsSource = countsProp ?? countsResp;
    if (!countsSource || !linksResp) return;

    try {
      const counts = (countsSource && (countsSource as any).data) ? (countsSource as any).data : countsSource ?? [];
      const groups = (linksResp && (linksResp as any).data && (linksResp as any).data.groups) ? (linksResp as any).data.groups : (linksResp?.groups ?? []);
      const ungrouped = (linksResp && (linksResp as any).data && (linksResp as any).data.ungrouped) ? (linksResp as any).data.ungrouped : (linksResp?.ungrouped ?? { links: [] });
      const links = ([] as any[]).concat(...(groups.map((g: any) => g.links ?? [])), ungrouped.links ?? []);

      const rowsLocal = (counts as any[]).map((c) => {
        const link = links.find((l: any) => l.id === c.linkId);
        return { linkId: c.linkId, name: link ? link.name : c.linkId, clicks: c.clicks };
      });

      setRows(rowsLocal);
    } catch (err) {
      console.warn('AnalyticsOverview build rows failed', err);
    }
  }, [countsResp, linksResp]);

  const total = useMemo(() => rows.reduce((s, r) => s + (r.clicks || 0), 0), [rows]);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <div className="text-2xl font-bold">{loadingCounts ? '...' : total}</div>
          <div className="text-sm text-muted-foreground">Total link clicks</div>
        </div>
        <div>
          <Button size="sm" variant="outline" onClick={() => { refetchCounts(); refetchLinks(); }}>
            Refresh
          </Button>
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full table-auto">
          <thead>
            <tr className="text-left text-sm text-muted-foreground">
              <th className="px-2 py-2">Link</th>
              <th className="px-2 py-2">Clicks</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.linkId} className="border-t">
                <td className="px-2 py-3">{r.name}</td>
                <td className="px-2 py-3 font-medium">{r.clicks}</td>
              </tr>
            ))}
            {rows.length === 0 && (
              <tr>
                <td colSpan={2} className="px-2 py-6 text-center text-sm text-muted-foreground">No analytics yet</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function MonthlyStats() {
  const [from, setFrom] = useState<string>(() => {
    const d = new Date();
    d.setDate(d.getDate() - 29);
    return d.toISOString().slice(0, 10);
  });
  const [to, setTo] = useState<string>(() => new Date().toISOString().slice(0, 10));

  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<any | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchSummary = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await apiService.get(`/analytics/my/summary?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`);
      setData(res.data?.data ?? res.data ?? null);
    } catch (err: any) {
      console.warn('MonthlyStats fetch failed', err);
      setError(err?.message || 'Failed to fetch');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSummary();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="mt-4 border-t pt-4">
      <div className="flex items-center gap-3 flex-wrap">
        <div className="flex items-center gap-2">
          <label className="text-sm text-muted-foreground">From</label>
          <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="flex items-center gap-2">
          <label className="text-sm text-muted-foreground">To</label>
          <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
        <div>
          <Button size="sm" onClick={fetchSummary} disabled={loading}>{loading ? 'Loading...' : 'Get'}</Button>
        </div>
      </div>

      {error && <p className="text-sm text-red-500 mt-3">{error}</p>}

      {data && (
        <div className="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className="p-4 rounded-lg border text-center">
            <div className="text-sm text-muted-foreground">Total Clicks</div>
            <div className="text-2xl font-bold">{data.totalClicks}</div>
            <div className="text-xs text-muted-foreground">{data.days} days ({new Date(data.from).toLocaleDateString()} - {new Date(data.to).toLocaleDateString()})</div>
          </div>
          <div className="p-4 rounded-lg border text-center">
            <div className="text-sm text-muted-foreground">Avg Clicks / Day</div>
            <div className="text-2xl font-bold">{Number(data.averagePerDay).toFixed(2)}</div>
            <div className="text-xs text-muted-foreground">Average over selected range</div>
          </div>
          <div className="p-4 rounded-lg border text-center">
            <div className="text-sm text-muted-foreground">Links</div>
            <div className="text-2xl font-bold">--</div>
            <div className="text-xs text-muted-foreground">(per-link breakdown above)</div>
          </div>
        </div>
      )}
    </div>
  );
}

function DeviceUsage() {
  const [data, setData] = useState<any | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchDevices = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await apiService.get('/analytics/my/devices');
      const payload = res.data?.data ?? res.data ?? null;
      setData(payload);
    } catch (err: any) {
      console.warn('DeviceUsage fetch failed', err);
      setError(err?.message || 'Failed to fetch device usage');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDevices();
  }, []);

  return (
    <div>
      <h3 className="text-lg font-medium mb-2 flex items-center gap-2">Device Usage</h3>
      <div className="p-4 rounded-lg border bg-muted/50">
        {loading ? (
          <div className="flex items-center gap-2 text-sm text-muted-foreground"><Loader2 className="h-4 w-4 animate-spin" /> Loading...</div>
        ) : error ? (
          <div className="text-sm text-red-500">{error}</div>
        ) : data ? (
          <div className="flex flex-col sm:flex-row items-stretch gap-4">
            {/* Left: table */}
            <div className="flex-1">
              <table className="w-full table-auto">
                <thead>
                  <tr className="text-left text-sm text-muted-foreground">
                    <th className="px-2 py-2">Device</th>
                    <th className="px-2 py-2">Count</th>
                    <th className="px-2 py-2">Percent</th>
                  </tr>
                </thead>
                <tbody>
                  {renderDeviceRow('Desktop', data.desktop ?? 0, data.total)}
                  {renderDeviceRow('Mobile', data.mobile ?? 0, data.total)}
                  {renderDeviceRow('Tablet', data.tablet ?? 0, data.total)}
                  {renderDeviceRow('Other', data.other ?? 0, data.total)}
                </tbody>
              </table>
            </div>

            {/* Right: pie chart */}
            <div className="w-full sm:w-48 flex-shrink-0 flex flex-col items-center justify-center">
              <PieDonut data={data} size={160} thickness={32} />
              <div className="mt-3 text-xs text-muted-foreground">Total: {data.total}</div>
            </div>
          </div>
        ) : (
          <div className="text-sm text-muted-foreground">No device analytics available</div>
        )}
        <div className="mt-3">
          <Button size="sm" variant="outline" onClick={fetchDevices} disabled={loading}>{loading ? 'Loading...' : 'Refresh'}</Button>
        </div>
      </div>
    </div>
  );
}

function renderDeviceRow(name: string, count: number, total: number) {
  const pct = total > 0 ? Math.round((count / total) * 100) : 0;
  return (
    <tr key={name} className="border-t">
      <td className="px-2 py-3">{name}</td>
      <td className="px-2 py-3 font-medium">{count}</td>
      <td className="px-2 py-3 text-sm text-muted-foreground">{pct}%</td>
    </tr>
  );
}

function PieDonut({ data, size = 120, thickness = 24 }: { data: any; size?: number; thickness?: number }) {
  const colors: Record<string, string> = {
    Desktop: '#4f46e5', // indigo
    Mobile: '#059669', // green
    Tablet: '#a78bfa', // purple
    Other: '#f59e0b' // amber
  };

  const segments = [
    { key: 'Desktop', value: data.desktop ?? 0 },
    { key: 'Mobile', value: data.mobile ?? 0 },
    { key: 'Tablet', value: data.tablet ?? 0 },
    { key: 'Other', value: data.other ?? 0 }
  ];

  const total = data.total ?? segments.reduce((s: number, seg) => s + seg.value, 0);

  const r = (size - thickness) / 2;
  const c = 2 * Math.PI * r;

  let offset = 0;

  return (
    <div className="flex flex-col items-center">
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
        <g transform={`translate(${size / 2}, ${size / 2})`}>
          {/* background circle */}
          <circle r={r} fill="transparent" stroke="#e6e6e6" strokeWidth={thickness} />

          {segments.map((seg) => {
            const value = seg.value || 0;
            const frac = total > 0 ? value / total : 0;
            const dash = frac * c;
            const dashArray = `${dash} ${c - dash}`;
            const strokeOffset = -offset;
            offset += dash;

            return (
              <circle
                key={seg.key}
                r={r}
                fill="transparent"
                stroke={colors[seg.key]}
                strokeWidth={thickness}
                strokeDasharray={dashArray}
                strokeDashoffset={strokeOffset}
                transform="rotate(-90)"
                style={{ transition: 'stroke-dasharray 300ms, stroke-dashoffset 300ms' }}
              />
            );
          })}
        </g>
      </svg>

      {/* Legend */}
      <div className="mt-3 grid grid-cols-2 gap-2 text-xs w-full">
        {segments.map((seg) => (
          <div key={seg.key} className="flex items-center gap-2">
            <span className="inline-block w-3 h-3 rounded-sm" style={{ background: colors[seg.key] }} />
            <span className="text-muted-foreground">{seg.key} ({seg.value ?? 0})</span>
          </div>
        ))}
      </div>
    </div>
  );
}