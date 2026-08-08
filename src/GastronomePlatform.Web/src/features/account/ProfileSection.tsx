import { Upload } from 'lucide-react';
import { useRef, useState } from 'react';
import { getErrorCode, getErrorMessage, getValidationError } from '../auth/apiErrors';
import { useUploadFileMutation } from '../media/mediaApi';
import {
  useChangeEmailMutation,
  useChangePhoneMutation,
  useChangeUserNameMutation,
  useMyProfileQuery,
  useSetVisibilityMutation,
  useUpdateAvatarMutation,
  useUpdateLocationMutation,
  useUpdatePersonalInfoMutation,
} from '../users/usersApi';
import { mediaThumbnailUrl } from '../../shared/api/media';
import type { Gender, UserProfileDto } from '../../shared/api/types/users';
import { userDisplayName } from '../../shared/api/types/users';
import { Button } from '../../shared/ui/Button';
import { SaveRow } from '../../shared/ui/SaveRow';
import { TextField } from '../../shared/ui/TextField';

const GENDER_OPTIONS: ReadonlyArray<{ value: Gender | ''; label: string }> = [
  { value: '', label: 'Не указано' },
  { value: 'Female', label: 'Женский' },
  { value: 'Male', label: 'Мужской' },
  { value: 'Other', label: 'Другое' },
  { value: 'PreferNotToSay', label: 'Предпочитаю не указывать' },
];

/** Пустая строка → null: backend трактует null как «очистить поле». */
function orNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}

/**
 * Раздел «Профиль» кабинета (макет AccountPages 4a): аватар (Media + PUT avatar),
 * личные данные, местоположение, приватность, учётные данные (email/телефон/никнейм)
 * с инлайн-редактированием и обработкой конфликтов уникальности.
 */
export function ProfileSection() {
  const { data: me, isLoading } = useMyProfileQuery();

  if (isLoading || !me) {
    return (
      <div className="animate-pulse space-y-5">
        <div className="h-40 rounded-card bg-sunken" />
        <div className="h-72 rounded-card bg-sunken" />
      </div>
    );
  }

  // key: при смене пользователя формы пересоздаются с новыми начальными значениями.
  return <ProfileForms key={me.userId} profile={me} />;
}

function ProfileForms({ profile }: { profile: UserProfileDto }) {
  return (
    <div className="flex max-w-[560px] flex-col gap-5">
      <AvatarCard profile={profile} />
      <PersonalInfoCard profile={profile} />
      <LocationCard profile={profile} />
      <PrivacyCard profile={profile} />
      <CredentialsCard profile={profile} />
    </div>
  );
}

/** Аватар: загрузка в Media (UserAvatar) + привязка через PUT me/avatar. */
function AvatarCard({ profile }: { profile: UserProfileDto }) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [uploadFile, { isLoading: isUploading }] = useUploadFileMutation();
  const [updateAvatar, { isLoading: isAttaching }] = useUpdateAvatarMutation();
  const [error, setError] = useState<string | null>(null);

  const busy = isUploading || isAttaching;

  async function handleFile(file: File) {
    setError(null);
    try {
      const { mediaId } = await uploadFile({ file, intendedEntityType: 'UserAvatar' }).unwrap();
      await updateAvatar(mediaId).unwrap();
    } catch (err) {
      setError(
        getErrorMessage(err) ??
          'Не получилось загрузить фото. Проверьте формат (JPG/PNG) и размер (до 10 МБ).',
      );
    }
  }

  return (
    <section className="rounded-card border border-line bg-surface p-6 shadow-card">
      <h2 className="font-semibold">Аватар</h2>
      <div className="mt-4 flex items-center gap-5">
        {profile.avatarMediaId ? (
          <img
            src={mediaThumbnailUrl(profile.avatarMediaId)}
            alt=""
            className="h-24 w-24 rounded-full object-cover ring-[6px] ring-surface shadow-card"
          />
        ) : (
          <span
            aria-hidden
            className="flex h-24 w-24 items-center justify-center rounded-full bg-saffron-100 text-3xl font-medium text-action"
          >
            {userDisplayName(profile).charAt(0).toUpperCase()}
          </span>
        )}
        <div>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/jpeg,image/png"
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) handleFile(file);
              e.target.value = '';
            }}
          />
          <Button variant="secondary" disabled={busy} onClick={() => fileInputRef.current?.click()}>
            <Upload className="mr-2 h-[18px] w-[18px]" strokeWidth={1.75} aria-hidden />
            {busy ? 'Загружаем…' : 'Загрузить фото'}
          </Button>
          <p className="mt-2 text-[13px] text-ink-muted">JPG или PNG до 10 МБ.</p>
          {error && <p className="mt-1 text-[13px] text-danger-text">{error}</p>}
        </div>
      </div>
    </section>
  );
}

/** Личные данные: ФИО, отображаемое имя, о себе, пол, дата рождения. */
function PersonalInfoCard({ profile }: { profile: UserProfileDto }) {
  const [firstName, setFirstName] = useState(profile.firstName ?? '');
  const [lastName, setLastName] = useState(profile.lastName ?? '');
  const [middleName, setMiddleName] = useState(profile.middleName ?? '');
  const [displayName, setDisplayName] = useState(profile.displayName ?? '');
  const [bio, setBio] = useState(profile.bio ?? '');
  const [gender, setGender] = useState<Gender | ''>((profile.gender as Gender | null) ?? '');
  const [dateOfBirth, setDateOfBirth] = useState(profile.dateOfBirth ?? '');

  const [save, { isLoading, isSuccess, error }] = useUpdatePersonalInfoMutation();

  function handleSave() {
    save({
      firstName: orNull(firstName),
      lastName: orNull(lastName),
      middleName: orNull(middleName),
      displayName: orNull(displayName),
      bio: orNull(bio),
      gender: gender === '' ? null : gender,
      dateOfBirth: dateOfBirth === '' ? null : dateOfBirth,
    });
  }

  return (
    <section className="rounded-card border border-line bg-surface p-6 shadow-card">
      <h2 className="font-semibold">Личные данные</h2>
      <div className="mt-4 flex flex-col gap-4">
        <TextField label="Имя" value={firstName} onChange={setFirstName} autoComplete="given-name" />
        <TextField label="Фамилия" value={lastName} onChange={setLastName} autoComplete="family-name" />
        <TextField label="Отчество" value={middleName} onChange={setMiddleName} />
        <TextField
          label="Отображаемое имя"
          value={displayName}
          onChange={setDisplayName}
          hint="Показывается вместо ФИО."
        />
        <div>
          <label htmlFor="profile-bio" className="mb-1.5 block text-sm font-medium text-ink">
            О себе
          </label>
          <textarea
            id="profile-bio"
            rows={3}
            value={bio}
            onChange={(e) => setBio(e.target.value)}
            placeholder="Пара слов о вашей кухне"
            className="w-full rounded-control border border-line bg-surface px-3.5 py-2.5 text-[15px] text-ink placeholder:text-ink-muted focus:border-action"
          />
        </div>
        <div className="flex flex-col gap-4 sm:flex-row">
          <div className="flex-1">
            <label htmlFor="profile-gender" className="mb-1.5 block text-sm font-medium text-ink">
              Пол
            </label>
            <select
              id="profile-gender"
              value={gender}
              onChange={(e) => setGender(e.target.value as Gender | '')}
              className="h-11 w-full cursor-pointer rounded-control border border-line bg-surface px-3 text-[15px] text-ink focus:border-action"
            >
              {GENDER_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
          <div className="flex-1">
            <label htmlFor="profile-dob" className="mb-1.5 block text-sm font-medium text-ink">
              Дата рождения
            </label>
            <input
              id="profile-dob"
              type="date"
              value={dateOfBirth}
              onChange={(e) => setDateOfBirth(e.target.value)}
              className="h-11 w-full rounded-control border border-line bg-surface px-3.5 text-[15px] text-ink focus:border-action"
            />
          </div>
        </div>
      </div>
      <SaveRow
        isLoading={isLoading}
        isSuccess={isSuccess}
        isError={error !== undefined}
        errorText={getErrorMessage(error) ?? undefined}
        onSave={handleSave}
      />
    </section>
  );
}

/** Местоположение: страна, регион, город. */
function LocationCard({ profile }: { profile: UserProfileDto }) {
  const [country, setCountry] = useState(profile.country ?? '');
  const [region, setRegion] = useState(profile.region ?? '');
  const [city, setCity] = useState(profile.city ?? '');

  const [save, { isLoading, isSuccess, error }] = useUpdateLocationMutation();

  return (
    <section className="rounded-card border border-line bg-surface p-6 shadow-card">
      <h2 className="font-semibold">Местоположение</h2>
      <div className="mt-4 flex flex-col gap-4">
        <TextField label="Страна" value={country} onChange={setCountry} autoComplete="country-name" />
        <div className="flex flex-col gap-4 sm:flex-row">
          <div className="flex-1">
            <TextField label="Регион" value={region} onChange={setRegion} />
          </div>
          <div className="flex-1">
            <TextField label="Город" value={city} onChange={setCity} autoComplete="address-level2" />
          </div>
        </div>
      </div>
      <SaveRow
        isLoading={isLoading}
        isSuccess={isSuccess}
        isError={error !== undefined}
        errorText={getErrorMessage(error) ?? undefined}
        onSave={() => save({ country: orNull(country), region: orNull(region), city: orNull(city) })}
      />
    </section>
  );
}

/** Приватность: свитч «Публичный профиль», сохраняется сразу по переключению. */
function PrivacyCard({ profile }: { profile: UserProfileDto }) {
  const [setVisibility, { isLoading }] = useSetVisibilityMutation();
  const isPublic = profile.isPublic;

  return (
    <section className="rounded-card border border-line bg-surface p-6 shadow-card">
      <h2 className="font-semibold">Приватность</h2>
      <div className="mt-4 flex items-start justify-between gap-4">
        <div>
          <p className="font-medium">Публичный профиль</p>
          <p className="mt-0.5 text-sm text-ink-secondary">
            Ваше имя и блюда видны посетителям платформы.
          </p>
        </div>
        <button
          type="button"
          role="switch"
          aria-checked={isPublic}
          disabled={isLoading}
          onClick={() => setVisibility(!isPublic)}
          className={`relative h-7 w-12 shrink-0 cursor-pointer rounded-pill transition-colors duration-[180ms] disabled:cursor-wait ${
            isPublic ? 'bg-action' : 'bg-line-strong'
          }`}
        >
          <span
            className={`absolute top-[3px] h-[22px] w-[22px] rounded-full bg-surface shadow-chip transition-[left] duration-[180ms] ${
              isPublic ? 'left-[23px]' : 'left-[3px]'
            }`}
          />
        </button>
      </div>
    </section>
  );
}

/** Учётные данные: email, телефон, никнейм — просмотр + инлайн-смена. */
function CredentialsCard({ profile }: { profile: UserProfileDto }) {
  const [changeEmail] = useChangeEmailMutation();
  const [changePhone] = useChangePhoneMutation();
  const [changeUserName] = useChangeUserNameMutation();

  return (
    <section className="rounded-card border border-line bg-surface p-6 shadow-card">
      <h2 className="font-semibold">Учётные данные</h2>
      <div className="mt-2">
        <CredentialRow
          label="Email"
          value={profile.email}
          inputType="email"
          autoComplete="email"
          takenCode="AUTH.EMAIL_TAKEN"
          takenMessage="Этот email уже используется."
          validationField="NewEmail"
          onSave={(v) => changeEmail(v).unwrap()}
        />
        <CredentialRow
          label="Телефон"
          value={profile.phone ?? ''}
          inputType="tel"
          autoComplete="tel"
          takenCode="AUTH.PHONE_TAKEN"
          takenMessage="Этот телефон уже используется."
          validationField="NewPhone"
          onSave={(v) => changePhone(v).unwrap()}
        />
        <CredentialRow
          label="Никнейм"
          value={profile.userName}
          inputType="text"
          autoComplete="username"
          takenCode="AUTH.USERNAME_TAKEN"
          takenMessage="Этот никнейм уже занят — попробуйте другой."
          validationField="NewUserName"
          onSave={(v) => changeUserName(v).unwrap()}
        />
      </div>
      <p className="mt-3.5 text-[13px] text-ink-muted">
        После смены никнейма старые упоминания не обновляются.
      </p>
    </section>
  );
}

interface CredentialRowProps {
  label: string;
  value: string;
  inputType: 'email' | 'tel' | 'text';
  autoComplete: string;
  takenCode: string;
  takenMessage: string;
  validationField: string;
  onSave: (newValue: string) => Promise<unknown>;
}

/** Строка учётного поля: «значение + Изменить» ↔ инлайн-форма с ошибками. */
function CredentialRow({
  label,
  value,
  inputType,
  autoComplete,
  takenCode,
  takenMessage,
  validationField,
  onSave,
}: CredentialRowProps) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(value);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSave() {
    setSaving(true);
    setError(null);
    try {
      await onSave(draft.trim());
      setEditing(false);
    } catch (err) {
      const code = getErrorCode(err);
      setError(
        code === takenCode
          ? takenMessage
          : getValidationError(err, validationField) ??
              getErrorMessage(err) ??
              'Не получилось сохранить. Проверьте значение.',
      );
    } finally {
      setSaving(false);
    }
  }

  if (!editing) {
    return (
      <div className="flex items-center gap-4 border-b border-line py-3">
        <div className="min-w-0 flex-1">
          <div className="text-[13px] text-ink-muted">{label}</div>
          <div className="tabular truncate font-medium">{value || '—'}</div>
        </div>
        <Button
          variant="secondary"
          size="sm"
          onClick={() => {
            setDraft(value);
            setError(null);
            setEditing(true);
          }}
        >
          Изменить
        </Button>
      </div>
    );
  }

  return (
    <div className="border-b border-line py-3">
      <TextField
        label={label}
        type={inputType}
        value={draft}
        onChange={setDraft}
        error={error}
        autoComplete={autoComplete}
        required
      />
      <div className="mt-3 flex gap-3">
        <Button variant="secondary" size="sm" disabled={saving} onClick={handleSave}>
          {saving ? 'Сохраняем…' : 'Сохранить'}
        </Button>
        <Button variant="ghost" size="sm" disabled={saving} onClick={() => setEditing(false)}>
          Отмена
        </Button>
      </div>
    </div>
  );
}

