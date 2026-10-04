import type { Geometry } from '../api/types';

/**
 * A schematic of an unfolded pawn, for the geometry choice. Proportions only —
 * not to scale, and not the calibration's values (§6 of CLAUDE.md): the real
 * dimensions live in calibration.json and the sheet shows them.
 */
export function GeometryIcon(props: { geometry: Geometry }) {
  const appendix = props.geometry === 'NoSupport' ? 0 : 8;
  const width = 26;
  const height = 50;
  const tab = 12;
  const total = 2 * appendix + 2 * height;
  const left = (width - tab) / 2;

  const outline =
    props.geometry === 'TabAndSocket'
      ? `M${left} 0 h${tab} v${appendix} H${width} v${2 * height} H${left + tab} v${appendix} h${-tab} v${-appendix} H0 v${-2 * height} H${left}Z`
      : `M0 0 H${width} V${total} H0Z`;

  return (
    <svg viewBox={`-2 -2 ${width + 4} ${total + 4}`} width="40" height="74" aria-hidden="true">
      <path d={outline} fill="var(--surface-2)" stroke="currentColor" strokeWidth="1" />
      <line x1="0" x2={width} y1={appendix + height} y2={appendix + height} stroke="currentColor" strokeDasharray="2 2" strokeWidth="0.8" />
    </svg>
  );
}
