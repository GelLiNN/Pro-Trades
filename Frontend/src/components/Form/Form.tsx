import {Box} from '@mui/material'

import {zodResolver} from '@hookform/resolvers/zod'
import {FieldValues, SubmitHandler, UseFormReturn, UseFormProps, useForm} from 'react-hook-form'

import type {BoxProps} from '@mui/material'
import type {ZodType, ZodTypeDef} from 'zod'

interface Props<TFormValues extends FieldValues, Schema>
  extends Omit<BoxProps, 'children' | 'onSubmit'> {
  children: (methods: UseFormReturn<TFormValues>) => React.ReactNode
  onSubmit: SubmitHandler<TFormValues>
  options?: UseFormProps<TFormValues>
  schema?: Schema
}

export const Form = <
  TFormValues extends FieldValues,
  Schema extends ZodType<unknown, ZodTypeDef, unknown> = ZodType<unknown, ZodTypeDef, unknown>,
>({
  children,
  onSubmit,
  options,
  schema,
  ...restProps
}: Props<TFormValues, Schema>) => {
  const methods = useForm<TFormValues>({
    ...options,
    resolver: schema ? zodResolver(schema) : undefined,
  })

  return (
    <Box component='form' noValidate onSubmit={methods.handleSubmit(onSubmit)} {...restProps}>
      {children(methods)}
    </Box>
  )
}
