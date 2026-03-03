import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
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
// Star rating
// ---------------------------------------------------------------------------

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

// ---------------------------------------------------------------------------
// Status badge
// ---------------------------------------------------------------------------

function StatusBadge({ status }: { status: string }) {
  const { t } = useTranslation('hr');
  switch (status) {
    case 'Draft':
      return (
        <Badge className="bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400">
          {t('reviews.status.Draft')}
        </Badge>
      );
    case 'InProgress':
      return (
        <Badge className="bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-300">
          {t('reviews.status.InProgress')}
        </Badge>
      );
    case 'Completed':
      return (
        <Badge className="bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-300">
          {t('reviews.status.Completed')}
        </Badge>
      );
    default:
      return <Badge variant="secondary">{status}</Badge>;
  }
}

// ---------------------------------------------------------------------------
// Feedback table
// ---------------------------------------------------------------------------

function FeedbackTable({ entries }: { entries: FeedbackEntry[] }) {
  const { t, i18n } = useTranslation('hr');

  function formatDate(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(i18n.language);
  }

  function respondentTypeLabel(type: string): string {
    switch (type) {
      case 'Self':          return t('reviews.respondentType.Self');
      case 'Peer':          return t('reviews.respondentType.Peer');
      case 'Manager':       return t('reviews.respondentType.Manager');
      case 'DirectReport':  return t('reviews.respondentType.DirectReport');
      default:              return type;
    }
  }

  if (entries.length === 0) {
    return (
      <p className="py-6 text-center text-sm text-muted-foreground">
        {t('reviews.noFeedback')}
      </p>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>{t('reviews.columns.type')}</TableHead>
          <TableHead>{t('reviews.columns.rating')}</TableHead>
          <TableHead>{t('reviews.feedback.comment')}</TableHead>
          <TableHead>{t('reviews.feedback.submittedAt')}</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {entries.map((entry) => (
          <TableRow key={entry.id}>
            <TableCell className="text-sm">
              {entry.isAnonymous ? (
                <span className="italic text-muted-foreground">
                  {t('reviews.feedback.anonymous_label')}
                </span>
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
  const { t } = useTranslation('hr');
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
          <Label>{t('reviews.feedback.type')}</Label>
          <Select value={respondentType} onValueChange={setRespondentType}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Self">{t('reviews.respondentType.Self')}</SelectItem>
              <SelectItem value="Peer">{t('reviews.respondentType.Peer')}</SelectItem>
              <SelectItem value="Manager">{t('reviews.respondentType.Manager')}</SelectItem>
              <SelectItem value="DirectReport">{t('reviews.respondentType.DirectReport')}</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-2">
          <Label htmlFor="rating">{t('reviews.feedback.rating')}</Label>
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
        <Label htmlFor="anonymous">{t('reviews.feedback.anonymous')}</Label>
      </div>

      <div className="space-y-2">
        <Label htmlFor="comments">{t('reviews.feedback.comment')}</Label>
        <Textarea
          id="comments"
          value={comments}
          onChange={(e) => setComments(e.target.value)}
          placeholder={t('reviews.feedback.commentPlaceholder')}
          rows={3}
          maxLength={2000}
        />
      </div>

      <div className="flex justify-end">
        <Button type="submit" disabled={submitFeedback.isPending}>
          {submitFeedback.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
          {t('reviews.feedback.submitButton')}
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
  const { t } = useTranslation('hr');
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
        <Label htmlFor="overallRating">{t('reviews.complete.overallRating')}</Label>
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
        <Label htmlFor="strengths">{t('reviews.complete.strengths')} *</Label>
        <Textarea
          id="strengths"
          value={strengthsNotes}
          onChange={(e) => setStrengthsNotes(e.target.value)}
          placeholder={t('reviews.complete.strengthsPlaceholder')}
          rows={3}
          maxLength={2000}
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="improvement">{t('reviews.complete.improvement')} *</Label>
        <Textarea
          id="improvement"
          value={improvementNotes}
          onChange={(e) => setImprovementNotes(e.target.value)}
          placeholder={t('reviews.complete.improvementPlaceholder')}
          rows={3}
          maxLength={2000}
          required
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor="goals">{t('reviews.complete.goals')} *</Label>
        <Textarea
          id="goals"
          value={goalsNotes}
          onChange={(e) => setGoalsNotes(e.target.value)}
          placeholder={t('reviews.complete.goalsPlaceholder')}
          rows={3}
          maxLength={2000}
          required
        />
      </div>
      <div className="flex justify-end">
        <Button type="submit" disabled={completeReview.isPending}>
          {completeReview.isPending && <Loader2 className="mr-1 h-4 w-4 animate-spin" />}
          {t('reviews.complete.completeButton')}
        </Button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Main Component
// ---------------------------------------------------------------------------

export function Component() {
  const { t, i18n } = useTranslation('hr');
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: review, isLoading } = useReview(id ?? '');

  function formatDate(iso: string | undefined): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString(i18n.language);
  }

  function reviewTypeLabel(type: string): string {
    switch (type) {
      case 'Annual':      return t('reviews.reviewType.Annual');
      case 'Probation':   return t('reviews.reviewType.Probation');
      case 'Quarterly':   return t('reviews.reviewType.Quarterly');
      case 'ThreeSixty':  return t('reviews.reviewType.ThreeSixty');
      default:            return type;
    }
  }

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
        {t('reviews.notFound')}
      </div>
    );
  }

  const isCompleted = review.status === 'Completed';

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('reviews.detailTitle', { name: review.employeeFullName })}
        actions={
          <div className="flex items-center gap-2">
            <StatusBadge status={review.status} />
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/hr/reviews')}
            >
              <ArrowLeft className="mr-1 h-4 w-4" />
              {t('common:buttons.back')}
            </Button>
          </div>
        }
      />

      {/* Metadata */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">{t('reviews.detailsCardTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-2 gap-6 sm:grid-cols-3">
            <DetailRow label={t('reviews.fields.employee')} value={review.employeeFullName || '—'} />
            <DetailRow label={t('reviews.fields.reviewer')} value={review.reviewerFullName || '—'} />
            <DetailRow label={t('reviews.fields.type')} value={reviewTypeLabel(review.reviewType)} />
            <DetailRow
              label={t('reviews.fields.period')}
              value={`${formatDate(review.reviewPeriodStart)} – ${formatDate(review.reviewPeriodEnd)}`}
            />
            <DetailRow label={t('reviews.fields.status')} value={<StatusBadge status={review.status} />} />
            {isCompleted && review.overallRating != null && (
              <DetailRow
                label={t('reviews.fields.overallRating')}
                value={<StarRating rating={review.overallRating} />}
              />
            )}
            {isCompleted && review.completedAt && (
              <DetailRow label={t('reviews.fields.completedAt')} value={formatDate(review.completedAt)} />
            )}
          </dl>
        </CardContent>
      </Card>

      {/* Completed notes */}
      {isCompleted && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t('reviews.notesCardTitle')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                {t('reviews.fields.strengths')}
              </p>
              <p className="mt-1 text-sm">{review.strengthsNotes || '—'}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                {t('reviews.fields.improvement')}
              </p>
              <p className="mt-1 text-sm">{review.improvementNotes || '—'}</p>
            </div>
            <div>
              <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                {t('reviews.fields.goals')}
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
            {t('reviews.feedbackCardTitle', { count: review.feedbackEntries.length })}
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
            <CardTitle className="text-base">{t('reviews.submitFeedbackCardTitle')}</CardTitle>
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
            <CardTitle className="text-base">{t('reviews.completeReviewCardTitle')}</CardTitle>
          </CardHeader>
          <CardContent>
            <CompleteReviewForm reviewId={review.id} />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
