import { useTranslation } from 'react-i18next';

/** A screen still to be written in this slice (§I.14, tasks 10 to 14). */
export function Placeholder(props: { title: string }) {
  const { t } = useTranslation();

  return (
    <div className="head">
      <h2>{t(props.title)}</h2>
      <p className="muted">{t('app.comingInThisSlice')}</p>
    </div>
  );
}
