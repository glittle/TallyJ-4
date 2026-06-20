import { onMounted, onUnmounted, ref, watch, type Ref } from "vue";

export interface UseViewportTableHeightOptions {
  /** Minimum height in pixels. Default 200. */
  min?: number;
  /** Extra space to leave below footer content. Default 8. */
  bottomMargin?: number;
  /** Footer element(s) rendered below the anchor (hints, toolbars, etc.). */
  bottomRef?: Ref<HTMLElement | null>;
  bottomRefs?: Ref<HTMLElement | null>[];
  /**
   * Element wrapping the anchor and any bottom footer siblings. Padding on
   * ancestors of this element (up to the container) is subtracted from height.
   * Defaults to the anchor's parent element.
   */
  paddingRootRef?: Ref<HTMLElement | null>;
  /** Scrollport whose bottom edge caps the available height. Default `#main-content`. */
  containerRef?: Ref<HTMLElement | null>;
  containerSelector?: string;
  /**
   * Layout elements to observe for size changes (in addition to the container,
   * anchor ancestors, and bottom footer elements).
   */
  observeSelectors?: string[];
}

function getElementVerticalFootprint(element: HTMLElement): number {
  const style = getComputedStyle(element);
  const marginTop = parseFloat(style.marginTop) || 0;
  const marginBottom = parseFloat(style.marginBottom) || 0;
  return marginTop + element.offsetHeight + marginBottom;
}

function sumPaddingBottomToContainer(
  paddingRoot: HTMLElement,
  container: HTMLElement,
): number {
  let total = 0;
  let node: HTMLElement | null = paddingRoot.parentElement;

  while (node) {
    total += parseFloat(getComputedStyle(node).paddingBottom) || 0;
    if (node === container) {
      break;
    }
    node = node.parentElement;
  }

  return total;
}

function resolveContainer(
  containerRef?: Ref<HTMLElement | null>,
  containerSelector = "#main-content",
): HTMLElement | null {
  return (
    containerRef?.value ??
    document.querySelector<HTMLElement>(containerSelector)
  );
}

function collectBottomRefs(
  bottomRef?: Ref<HTMLElement | null>,
  bottomRefs?: Ref<HTMLElement | null>[],
): Ref<HTMLElement | null>[] {
  const refs: Ref<HTMLElement | null>[] = [];
  if (bottomRef) {
    refs.push(bottomRef);
  }
  if (bottomRefs?.length) {
    refs.push(...bottomRefs);
  }
  return refs;
}

/**
 * Computes a dynamic height for Element Plus tables (or similar scroll regions)
 * so they fill the visible area inside a layout container without causing page
 * overflow.
 *
 * @example
 * const tableWrapperRef = ref<HTMLElement | null>(null);
 * const footerRef = ref<HTMLElement | null>(null);
 * const sectionRef = ref<HTMLElement | null>(null);
 * const { height: tableHeight, remeasure } = useViewportTableHeight(tableWrapperRef, {
 *   paddingRootRef: sectionRef,
 *   bottomRef: footerRef,
 * });
 */
export function useViewportTableHeight(
  anchorRef: Ref<HTMLElement | null>,
  options: UseViewportTableHeightOptions = {},
) {
  const {
    min = 200,
    bottomMargin = 8,
    bottomRef,
    bottomRefs,
    paddingRootRef,
    containerRef,
    containerSelector = "#main-content",
    observeSelectors = [".main-layout .el-header"],
  } = options;

  const height = ref(min);
  const footerRefs = collectBottomRefs(bottomRef, bottomRefs);

  let frame = 0;
  let observer: ResizeObserver | null = null;
  const observedNodes = new Set<Element>();

  const observeNode = (node: Element | null | undefined) => {
    if (!observer || !node || observedNodes.has(node)) {
      return;
    }
    observer.observe(node);
    observedNodes.add(node);
  };

  const resolvePaddingRoot = (anchor: HTMLElement): HTMLElement =>
    paddingRootRef?.value ?? anchor.parentElement ?? anchor;

  const measure = () => {
    cancelAnimationFrame(frame);
    frame = requestAnimationFrame(() => {
      const anchor = anchorRef.value;
      if (!anchor) {
        return;
      }

      const container = resolveContainer(containerRef, containerSelector);
      const containerBottom =
        container?.getBoundingClientRect().bottom ?? window.innerHeight;
      const anchorTop = anchor.getBoundingClientRect().top;

      let bottomInset = bottomMargin;

      for (const footerRef of footerRefs) {
        if (footerRef.value) {
          bottomInset += getElementVerticalFootprint(footerRef.value);
        }
      }

      if (container) {
        bottomInset += sumPaddingBottomToContainer(
          resolvePaddingRoot(anchor),
          container,
        );
      }

      const next = Math.floor(containerBottom - anchorTop - bottomInset);
      height.value = Math.max(min, next);
    });
  };

  const observeAnchorAncestors = (anchor: HTMLElement) => {
    observeNode(anchor);

    const container = resolveContainer(containerRef, containerSelector);
    let node: HTMLElement | null = anchor.parentElement;
    while (node) {
      observeNode(node);
      if (node === container) {
        break;
      }
      node = node.parentElement;
    }
  };

  onMounted(() => {
    window.addEventListener("resize", measure);
    observer = new ResizeObserver(measure);

    observeNode(resolveContainer(containerRef, containerSelector));

    for (const selector of observeSelectors) {
      observeNode(document.querySelector(selector));
    }

    watch(
      anchorRef,
      (anchor) => {
        if (!anchor) {
          return;
        }
        observeAnchorAncestors(anchor);
        measure();
      },
      { immediate: true },
    );

    watch(
      () => footerRefs.map((footer) => footer.value),
      (elements) => {
        for (const element of elements) {
          observeNode(element);
        }
        measure();
      },
      { immediate: true, deep: true },
    );

    if (paddingRootRef) {
      watch(
        paddingRootRef,
        (root) => {
          observeNode(root);
          measure();
        },
        { immediate: true },
      );
    }

    if (containerRef) {
      watch(
        containerRef,
        (container) => {
          observeNode(container);
          measure();
        },
        { immediate: true },
      );
    }
  });

  onUnmounted(() => {
    window.removeEventListener("resize", measure);
    observer?.disconnect();
    observedNodes.clear();
    cancelAnimationFrame(frame);
  });

  return { height, remeasure: measure };
}
