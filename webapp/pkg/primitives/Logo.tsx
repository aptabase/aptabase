type Props = {
  className?: string;
};

export function Logo(props: Props) {
  return (
    <svg viewBox="0 0 1000 760" className={props.className}>
      <path
        fill="currentColor"
        d="m626.7,177.36c-55.8-98.4-197.59-98.4-253.39,0L112.97,636.44h387.03c0-51.67,41.88-93.55,93.55-93.55h22.09l57.82,93.55h213.57l-260.34-459.07Zm-11.06,365.52l-70.21-123.82c-20.01-35.28-70.84-35.28-90.85,0l-70.21,123.82h-110.79l181.01-319.19c20.01-35.28,70.84-35.28,90.85,0l181.01,319.19h-110.79Z"
      ></path>
    </svg>
  );
}
