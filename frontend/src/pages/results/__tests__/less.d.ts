declare module "less" {
  interface RenderOutput {
    css: string;
  }

  interface Less {
    render(input: string): Promise<RenderOutput>;
  }

  const less: Less;
  export default less;
}
