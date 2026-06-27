import { cva, type VariantProps } from 'class-variance-authority';

export const formFieldVariants = cva('grid gap-2');

export const formLabelVariants = cva(
  'text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70',
  {
    variants: {
      zRequired: {
        true: "after:content-[''] after:inline-block after:w-1.5 after:h-1.5 after:rounded-full after:bg-primary/70 after:ml-1.5 after:mb-0.5 after:align-middle",
      },
    },
  },
);

export const formControlVariants = cva('');

export const formMessageVariants = cva('text-sm', {
  variants: {
    zType: {
      default: 'text-muted-foreground',
      error: 'text-red-500',
      success: 'text-green-500',
      warning: 'text-yellow-500',
    },
  },
  defaultVariants: {
    zType: 'default',
  },
});

export type ZardFormMessageTypeVariants = NonNullable<VariantProps<typeof formMessageVariants>['zType']>;
