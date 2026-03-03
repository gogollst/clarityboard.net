import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useReviews } from '@/hooks/useHr';
import PageHeader from '@/components/shared/PageHeader';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { Card, CardContent } from '@/components/ui/card';
import { Star, ChevronLeft, ChevronRight } from 'lucide-react';
import type { PerformanceReview } from '@/types/hr';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formatDate(iso: string | undefined): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('de-DE');
}

function reviewTypeLabel(type: string): string {
  switch (type) {
    case 'Annual':      return 'Jährlich';
    case 'Probation':   return 'Probezeit';
    case 'Quarterly':   return 'Quartal';
    case 'ThreeSixty':  return '360°';
    default:            return type;
  }
}

function StatusBadge({ status }: { status: string }) {
  switch (status) {
    case 'Draft':
      return (
        <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
          Entwurf
        </Badge>
      );
    case 'InProgress':
      return (
        <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300">
          In Bearbeitung
        </Badge>
      );
    case 'Completed':
      return (
        <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
          Abgeschlossen
        </Badge>
      );
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
}

function RatingDisplay({ rating }: { rating?: number }) {
  if (rating == null) return <span className="text-muted-foreground">—</span>;
  return (
    <span className="flex items-center gap-1 tabular-nums">
      <Star className="h-3.5 w-3.5 fill-amber-400 text-amber-400" />
      {rating}/5
    </span>
  );
}

// ---------------------------------------------------------------------------
// Review row
// ---------------------------------------------------------------------------

interface ReviewRowProps {
  review: PerformanceReview;
  onDetails: (id: string) => void;
}

function ReviewRow({ review, onDetails }: ReviewRowProps) {
  return (
    <TableRow>
      <TableCell className="font-medium">{review.employeeFullName || '—'}</TableCell>
      <TableCell className="text-sm text-muted-foreground">{review.reviewerFullName || '—'}</TableCell>
      <TableCell className="text-sm">
        {formatDate(review.reviewPeriodStart)} – {formatDate(review.reviewPeriodEnd)}
      </TableCell>
      <TableCell className="text-sm">{reviewTypeLabel(review.reviewType)}</TableCell>
      <TableCell>
        <StatusBadge status={review.status} />
      </TableCell>
      <TableCell>
        <RatingDisplay rating={review.overallRating} />
      </TableCell>
      <TableCell className="text-sm text-muted-foreground">
        {formatDate(review.createdAt)}
      </TableCell>
      <TableCell>
        <Button variant="ghost" size="sm" onClick={() => onDetails(review.id)}>
          Details
        </Button>
      </TableCell>
    </TableRow>
  );
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const navigate = useNavigate();
  const [reviewType, setReviewType] = useState<string>('');
  const [status, setStatus] = useState<string>('');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { data, isLoading } = useReviews({
    reviewType: reviewType || undefined,
    status: status || undefined,
    page,
    pageSize,
  });

  const reviews = data?.items ?? [];
  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 1;

  return (
    <div>
      <PageHeader title="Beurteilungen" />

      {/* Filters */}
      <div className="mb-4 flex flex-wrap gap-3">
        <Select
          value={reviewType || 'all'}
          onValueChange={(v) => { setReviewType(v === 'all' ? '' : v); setPage(1); }}
        >
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Typ" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Alle Typen</SelectItem>
            <SelectItem value="Annual">Jährlich</SelectItem>
            <SelectItem value="Probation">Probezeit</SelectItem>
            <SelectItem value="Quarterly">Quartal</SelectItem>
            <SelectItem value="ThreeSixty">360°</SelectItem>
          </SelectContent>
        </Select>

        <Select
          value={status || 'all'}
          onValueChange={(v) => { setStatus(v === 'all' ? '' : v); setPage(1); }}
        >
          <SelectTrigger className="w-44">
            <SelectValue placeholder="Status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Alle Status</SelectItem>
            <SelectItem value="Draft">Entwurf</SelectItem>
            <SelectItem value="InProgress">In Bearbeitung</SelectItem>
            <SelectItem value="Completed">Abgeschlossen</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="space-y-2 p-4">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : reviews.length === 0 ? (
            <p className="py-12 text-center text-sm text-muted-foreground">
              Keine Beurteilungen vorhanden.
            </p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Mitarbeiter</TableHead>
                  <TableHead>Beurteiler</TableHead>
                  <TableHead>Zeitraum</TableHead>
                  <TableHead>Typ</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Bewertung</TableHead>
                  <TableHead>Erstellt am</TableHead>
                  <TableHead />
                </TableRow>
              </TableHeader>
              <TableBody>
                {reviews.map((review) => (
                  <ReviewRow
                    key={review.id}
                    review={review}
                    onDetails={(id) => navigate(`/hr/reviews/${id}`)}
                  />
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="mt-4 flex items-center justify-between">
          <span className="text-sm text-muted-foreground">
            Seite {page} von {totalPages} ({data?.totalCount ?? 0} Einträge)
          </span>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
