import { useRef, useState } from 'react';
import { Loader2, Trash2, Upload } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Button } from '@/components/ui/button';
import { useUploadAvatar, useDeleteAvatar } from '@/hooks/useSettings';

interface AvatarSectionProps {
  avatarUrl: string | null;
  firstName: string;
  lastName: string;
}

export function AvatarSection({ avatarUrl, firstName, lastName }: AvatarSectionProps) {
  const { t } = useTranslation('settings');
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const uploadAvatar = useUploadAvatar();
  const deleteAvatar = useDeleteAvatar();

  const initials = `${firstName?.[0] ?? ''}${lastName?.[0] ?? ''}`.toUpperCase();

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      return;
    }

    const objectUrl = URL.createObjectURL(file);
    setPreview(objectUrl);

    try {
      await uploadAvatar.mutateAsync(file);
    } finally {
      setPreview(null);
      URL.revokeObjectURL(objectUrl);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleDelete = async () => {
    await deleteAvatar.mutateAsync();
  };

  const displayUrl = preview ?? avatarUrl;
  const isLoading = uploadAvatar.isPending || deleteAvatar.isPending;

  return (
    <div className="flex items-center gap-4">
      <Avatar className="h-20 w-20">
        {displayUrl ? (
          <AvatarImage src={displayUrl} alt={`${firstName} ${lastName}`} />
        ) : null}
        <AvatarFallback className="text-lg">{initials}</AvatarFallback>
      </Avatar>

      <div className="flex flex-col gap-2">
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={isLoading}
            onClick={() => fileInputRef.current?.click()}
          >
            {uploadAvatar.isPending ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <Upload className="h-4 w-4" />
            )}
            {t('avatar.upload')}
          </Button>

          {avatarUrl && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isLoading}
              onClick={handleDelete}
            >
              {deleteAvatar.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <Trash2 className="h-4 w-4" />
              )}
              {t('avatar.remove')}
            </Button>
          )}
        </div>

        <p className="text-xs text-muted-foreground">
          {t('avatar.hint')}
        </p>
      </div>

      <input
        ref={fileInputRef}
        type="file"
        accept="image/jpeg,image/png"
        className="hidden"
        onChange={handleFileChange}
      />
    </div>
  );
}
