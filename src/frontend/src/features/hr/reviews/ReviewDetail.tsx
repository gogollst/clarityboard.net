import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useReview, useSubmitFeedback, useCompleteReview } from '@/hooks/useHr';
import PageHeader from '@/components/shared/PageHeader';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Skeleton } from '@/components/ui/skeleton';
import { ArrowLeft, Loader2 } from 'lucide-react';
import type { FeedbackEntry } from '@/types/hr';

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

function respondentTypeLabel(type: string): string {
  switch (type) {
    case 'Self':          return 'Selbstbeurteilung';
    case 'Peer':          return 'Peer';
    case 'Manager':       return 'Vorgesetzter';
    case 'DirectReport':  return 'Direkter Bericht';
    default:              return type;
  }
}

function StarRating({ rating, max = 5 }: { rating: number; max?: number }) {
  return (
    <span className="text-base tracking-tight">
      {Array.from({ length: max }).map((_, i) => (
        <span key={i} className={i < rating ? 'text-amber-400' : 'text-muted-foreground/30'}>
          {i < rating ? '★' : '☆'}
        </span>
      ))}
    </span>
  );
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

// ---------------------------------------------------------------------------
// Detail row helper
// ---------------------------------------------------------------------------

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        {label}
      </dt>
      <dd className="mt-1 text-sm font-medium text-foreground">{value}</dd>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Feedback table
// ---------------------------------------------------------------------------

function FeedbackTable({ entries }: { entries: FeedbackEntry[] }) {
  if (entries.length === 0) {
    return (
      <p className="py-6 text-center text-sm text-muted-foreground">
        Noch kein Feedback eingereicht.
      </p>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Typ</TableHead>
          <TableHead>Bewertung</TableHead>
          <TableHead>Kommentar</TableHead>
          <TableHead>Eingereicht am</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {entries.map((entry) => (
          <TableRow key={entry.id}>
            <TableCell className="text-sm">
              {entry.isAnonymous ? (
                <span className="italic text-muted-foreground">Anonym</span>
              ) : (
                respondentTypeLabel(entry.respondentType)
              )}
            </TableCell>
            <TableCell>
              <StarRating rating={entry.rating} />
            </TableCell>
            <TableCell className="max-w-xs text-sm text-muted-foreground">
              {entry.comments || '—'}
            </TableCell>
            <TableCell className="text-sm text-muted-foreground">
              {formatDate(entry.submittedAt)}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

// ---------------------------------------------------------------------------
// Feedback form
// ---------------------------------------------------------------------------

interface FeedbackFormProps {
  reviewId: string;
}

function FeedbackForm({ reviewId }: FeedbackFormProps) {
  const submitFeedback = useSubmitFeedback(reviewId);
  const [respondentType, setRespondentType] = useState('Self');
  const [isAnonymous, setIsAnonymous] = useState(false);
  const [rating, setRating] = useState(3);
  const [comments, setComments] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    submitFeedback.mutate({ respondentType, isAnonymous, rating, comments: comments || undefined });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-2">
          <Label>Feedbacktyp</Label>
          <Select value={respondentType} onValueChange={setRespondentType}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Self">Selbstbeurteilung</SelectItem>
              <SelectItem value="Peer">Peer</SelectItem>
              <SelectItem value="Manager">Vorgesetzter</SelectItem>
              <SelectItem value="DirectReport">Direkter Bericht</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <Label htmlFor="rating">Bewertung (1-5)</Label>
          <Input
            id="rating"
            type="number"
            min={1}
            max={5}
            value={rating}
            onChange={(e) => setRating(Number(e.target.value))}
            required
          />
        </div>
      </div>

      <div className="flex items-center gap-2">
        <input
          id="anonymous"
          type="checkbox"
          checked={isAnonymous}
          onChange={(e) => setIsAnonymous(e.target.checked)}
          className="h-4 w-4 rounded border-input"
        />
        <Label htmlFor="anonymous">Anonym einreichen</Label>
      </div>

      <div className="space-y-2">
        <Label htmlFor="comments">Kommentar (optional)</Label>
        <Textarea
          id="comments"
          value={comments}
          onChange={(e) => setComments(e.target.value)}
          placeholder="Feedback eingeben..."
          rows={3}
          maxLength={2000}
        />
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={submitFeedback.isPending}>
          {submitFeedback.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
          Feedback einreichen
        </Button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Complete review form
// ---------------------------------------------------------------------------

interface CompleteReviewFormProps {
  reviewId: string;
}

function CompleteReviewForm({ reviewId }: CompleteReviewFormProps) {
  const completeReview = useCompleteReview(reviewId);
  const [overallRating, setOverallRating] = useState(3);
  const [strengthsNotes, setStrengthsNotes] = useState('');
  const [improvementNotes, setImprovementNotes] = useState('');
  const [goalsNotes, setGoalsNotes] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!strengthsNotes || !improvementNotes || !goalsNotes) return;
    completeReview.mutate({ overallRating, strengthsNotes, improvementNotes, goalsNotes });
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="overallRating">Gesamtbewertung (1-5)</Label>
        <Input
          id="overallRating"
          type="number"
          min={1}
          max={5}
          value={overallRating}
          onChange={(e) => setOverallRating(Number(e.target.value))}
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="strengths">Stärken *</Label>
        <Textarea
          id="strengths"
          value={strengthsNotes}
          onChange={(e) => setStrengthsNotes(e.target.value)}
          placeholder="Stärken des Mitarbeiters..."
          rows={3}
          maxLength={2000}
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="improvement">Verbesserungspotenzial *</Label>
        <Textarea
          id="improvement"
          value={improvementNotes}
          onChange={(e) => setImprovementNotes(e.target.value)}
          placeholder="Bereiche mit Entwicklungspotenzial..."
          rows={3}
          maxLength={2000}
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="goals">Ziele *</Label>
        <Textarea
          id="goals"
          value={goalsNotes}
          onChange={(e) => setGoalsNotes(e.target.value)}
          placeholder="Ziele für die nächste Periode..."
          rows={3}
          maxLength={2000}
          required
        />
      </div>
      <div className="flex justify-end">
        <Button type="submit" disabled={completeReview.isPending}>
          {completeReview.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
          Beurteilung abschließen
        </Button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: review, isLoading } = useReview(id ?? '');

  if (isLoading) {
    return (
      <div>
        <Skeleton className="mb-6 h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!review) {
    return (
      <div className="py-12 text-center text-muted-foreground">
        Beurteilung nicht gefunden.
      </div>
    );
  }

  const isCompleted = review.status === 'Completed';

  return (
    <div className="space-y-6">
      <PageHeader
        title={`Beurteilung: ${review.employeeFullName}`}
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge status={review.status} />
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/hr/reviews')}
            >
              <ArrowLeft className="mr-1 h-4 w-4" />
              Zurück
            </Button>
          </div>
        }
      />

      {/* Metadata */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Details</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-2 gap-6 sm:grid-cols-3">
            <DetailRow label="Mitarbeiter" value={review.employeeFullName || '—'} />
            <DetailRow label="Beurteiler" value={review.reviewerFullName || '—'} />
            <DetailRow label="Typ" value={reviewTypeLabel(review.reviewType)} />
            <DetailRow
              label="Zeitraum"
              value={`${formatDate(review.reviewPeriodStart)} – ${formatDate(review.reviewPeriodEnd)}`}
            />
            <DetailRow label="Status" value={<StatusBadge status={review.status} />} />
            {isCompleted && review.overallRating != null && (
              <DetailRow
                label="Gesamtbewertung"
                value={<StarRating rating={review.overallRating} />}
              />
            )}
            {isCompleted && review.completedAt && (
              <DetailRow label="Abgeschlossen am" value={formatDate(review.completedAt)} />
            )}
          </dl>
        </CardContent>
      </Card>

      {/* Completed notes */}
      {isCompleted && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Beurteilungsnotizen</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                Stärken
              </p>
              <p className="mt-1 text-sm">{review.strengthsNotes || '—'}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                Verbesserungspotenzial
              </p>
              <p className="mt-1 text-sm">{review.improvementNotes || '—'}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                Ziele
              </p>
              <p className="mt-1 text-sm">{review.goalsNotes || '—'}</p>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Feedback entries */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">
            Feedback ({review.feedbackEntries.length})
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <FeedbackTable entries={review.feedbackEntries} />
        </CardContent>
      </Card>

      {/* Submit feedback form (only when not completed) */}
      {!isCompleted && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Feedback einreichen</CardTitle>
          </CardHeader>
          <CardContent>
            <FeedbackForm reviewId={review.id} />
          </CardContent>
        </Card>
      )}

      {/* Complete review form (only when not completed) */}
      {!isCompleted && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Beurteilung abschließen</CardTitle>
          </CardHeader>
          <CardContent>
            <CompleteReviewForm reviewId={review.id} />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
