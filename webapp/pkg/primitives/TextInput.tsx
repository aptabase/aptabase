type Props = {
  name: string;
  label: string;
  required?: boolean;
  type?: "text" | "email";
  placeholder?: string;
  autoComplete?: string;
  value: string;
  className?: string;
  onChange?: (value: string) => void;
};

export function TextInput(props: Props) {
  const onChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    props.onChange?.(event.target.value);
  };

  const type = props.type ?? "text";

  return (
    <div>
      <label htmlFor={props.name} className="block text-sm mb-1">
        {props.label}
      </label>
      <input
        className="form-input w-full"
        name={props.name}
        type={type}
        required={props.required}
        placeholder={props.placeholder}
        autoComplete={props.autoComplete}
        value={props.value}
        onChange={onChange}
      />
    </div>
  );
}
